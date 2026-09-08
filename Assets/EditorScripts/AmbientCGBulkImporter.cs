using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AmbientCGBulkImporter : MonoBehaviour
{
    [Header("Folders")]
    public string texturesFolder = "Assets/AmbientCG";
    public string materialsFolder = "Assets/AmbientCG-Materials";

    [Header("Download")]
    public bool overwriteExistingDownloads = false;
    public bool overwriteExistingMaterials = true;
    public bool preferPngArchives = true;

    [Header("Material")]
    [Range(0.005f, 0.08f)]
    public float parallaxStrength = 0.02f;

    [Range(0f, 1f)]
    public float occlusionStrength = 1f;

    [Header("Safety")]
    public bool skipAssetsThatAlreadyHaveAMaterial = false;
}

#if UNITY_EDITOR
[CustomEditor(typeof(AmbientCGBulkImporter))]
public class AmbientCGBulkImporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(AmbientCGBulkImporterRunner.IsRunning))
        {
            if (GUILayout.Button("Download Assets", GUILayout.Height(32)))
            {
                AmbientCGBulkImporterRunner.Run((AmbientCGBulkImporter)target);
            }
        }

        if (AmbientCGBulkImporterRunner.IsRunning)
        {
            EditorGUILayout.HelpBox("Import em andamento.", MessageType.Info);
        }
    }
}

internal static class AmbientCGBulkImporterRunner
{
    private const string ApiBaseUrl = "https://ambientcg.com/api/v3/assets";
    private static readonly Regex IdRegex = new Regex("\"id\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex TotalRegex = new Regex("\"totalResults\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);

    public static bool IsRunning { get; private set; }

    public static void Run(AmbientCGBulkImporter settings)
    {
        if (IsRunning || settings == null)
            return;

        IsRunning = true;

        try
        {
            RunInternal(settings);
        }
        catch (Exception ex)
        {
            Debug.LogError("[AmbientCG] Falha geral: " + ex);
        }
        finally
        {
            IsRunning = false;
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void RunInternal(AmbientCGBulkImporter settings)
    {
        string texturesFolder = NormalizeAssetFolder(settings.texturesFolder, "Assets/AmbientCG");
        string materialsFolder = NormalizeAssetFolder(settings.materialsFolder, "Assets/AmbientCG-Materials");

        EnsureDirectoryAbsolute(ToAbsolutePath(texturesFolder));
        EnsureDirectoryAbsolute(ToAbsolutePath(materialsFolder));

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
            throw new InvalidOperationException("Shader 'Universal Render Pipeline/Lit' não encontrado. Verifique se o projeto está usando URP.");

        EditorUtility.DisplayProgressBar("ambientCG", "Lendo catálogo...", 0f);
        List<string> assetIds = FetchAllMaterialIds();

        if (assetIds.Count == 0)
        {
            Debug.LogWarning("[AmbientCG] Nenhum asset material foi encontrado.");
            return;
        }

        Debug.Log("[AmbientCG] IDs encontrados: " + assetIds.Count);

        DownloadAndExtractAll(assetIds, texturesFolder, materialsFolder, settings);

        EditorUtility.DisplayProgressBar("ambientCG", "Atualizando AssetDatabase...", 0.92f);
        AssetDatabase.Refresh();

        CreateOrUpdateAllMaterials(texturesFolder, materialsFolder, litShader, settings);

        Debug.Log("[AmbientCG] Processo concluído.");
    }

    private static List<string> FetchAllMaterialIds()
    {
        List<string> result = new List<string>();
        HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int offset = 0;
        int pageSize = 500;
        int totalResults = -1;

        while (true)
        {
            string url = ApiBaseUrl + "?type=material&limit=" + pageSize + "&offset=" + offset;
            string json = DownloadString(url);

            if (string.IsNullOrEmpty(json))
                break;

            if (totalResults < 0)
            {
                Match totalMatch = TotalRegex.Match(json);
                if (totalMatch.Success)
                {
                    int.TryParse(totalMatch.Groups[1].Value, out totalResults);
                }
            }

            MatchCollection matches = IdRegex.Matches(json);
            int addedThisPage = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                string id = matches[i].Groups[1].Value;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (unique.Add(id))
                {
                    result.Add(id);
                    addedThisPage++;
                }
            }

            if (addedThisPage == 0)
                break;

            offset += addedThisPage;

            if (totalResults > 0)
            {
                float progress = Mathf.Clamp01(offset / (float)totalResults) * 0.18f;
                EditorUtility.DisplayProgressBar("ambientCG", "Catálogo: " + offset + "/" + totalResults, progress);
            }
            else
            {
                EditorUtility.DisplayProgressBar("ambientCG", "Catálogo: " + offset, 0.1f);
            }

            if (addedThisPage < pageSize)
                break;
        }

        return result;
    }

    private static void DownloadAndExtractAll(
        List<string> assetIds,
        string texturesFolder,
        string materialsFolder,
        AmbientCGBulkImporter settings)
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "AmbientCG-Unity-Temp");
        EnsureDirectoryAbsolute(tempRoot);

        int total = assetIds.Count;
        int skipped = 0;
        int failed = 0;
        int downloaded = 0;
        int notFound = 0;

        for (int i = 0; i < assetIds.Count; i++)
        {
            string assetId = assetIds[i];

            string assetFolder = CombineAssetPath(texturesFolder, assetId);
            string materialPath = CombineAssetPath(materialsFolder, assetId + ".mat");
            string assetFolderAbsolute = ToAbsolutePath(assetFolder);

            bool materialExists = File.Exists(ToAbsolutePath(materialPath));
            if (settings.skipAssetsThatAlreadyHaveAMaterial && materialExists)
            {
                skipped++;
                continue;
            }

            if (Directory.Exists(assetFolderAbsolute) && !settings.overwriteExistingDownloads)
            {
                skipped++;
                continue;
            }

            float progress = 0.18f + ((i + 1) / (float)Math.Max(1, total)) * 0.72f;
            EditorUtility.DisplayProgressBar("ambientCG", "Baixando " + (i + 1) + "/" + total + ": " + assetId, progress);

            string zipPath = Path.Combine(tempRoot, assetId + ".zip");

            try
            {
                bool ok = TryDownloadBestArchive(assetId, zipPath, settings.preferPngArchives);
                if (!ok)
                {
                    notFound++;
                    continue;
                }

                if (Directory.Exists(assetFolderAbsolute))
                    Directory.Delete(assetFolderAbsolute, true);

                Directory.CreateDirectory(assetFolderAbsolute);
                ExtractZip(zipPath, assetFolderAbsolute);
                downloaded++;
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogWarning("[AmbientCG] Falha ao baixar/extrair " + assetId + ": " + ex.Message);
            }
            finally
            {
                TryDeleteFile(zipPath);
            }
        }

        Debug.Log(
            "[AmbientCG] Downloads finalizados. " +
            "Baixados: " + downloaded +
            ", pulados: " + skipped +
            ", sem 1K/2K: " + notFound +
            ", falhas: " + failed + "."
        );
    }

    private static bool TryDownloadBestArchive(string assetId, string destinationZipPath, bool preferPng)
    {
        List<string> suffixes = new List<string>();

        if (preferPng)
        {
            suffixes.Add("1K-PNG");
            suffixes.Add("1K-JPG");
            suffixes.Add("2K-PNG");
            suffixes.Add("2K-JPG");
        }
        else
        {
            suffixes.Add("1K-JPG");
            suffixes.Add("1K-PNG");
            suffixes.Add("2K-JPG");
            suffixes.Add("2K-PNG");
        }

        for (int i = 0; i < suffixes.Count; i++)
        {
            string fileName = assetId + "_" + suffixes[i] + ".zip";
            string url = "https://ambientcg.com/get?file=" + fileName;

            try
            {
                DownloadFile(url, destinationZipPath);

                FileInfo info = new FileInfo(destinationZipPath);
                if (info.Exists && info.Length > 1024)
                {
                    Debug.Log("[AmbientCG] Baixado: " + fileName);
                    return true;
                }
            }
            catch (WebException webEx)
            {
                HttpWebResponse response = webEx.Response as HttpWebResponse;
                if (response != null)
                {
                    int code = (int)response.StatusCode;
                    if (code == 404 || code == 403)
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // tenta próxima variante
            }
            finally
            {
                if (File.Exists(destinationZipPath))
                {
                    FileInfo tempInfo = new FileInfo(destinationZipPath);
                    if (tempInfo.Length <= 1024)
                        TryDeleteFile(destinationZipPath);
                }
            }
        }

        return false;
    }

    private static string DownloadString(string url)
    {
        using (WebClient client = CreateWebClient())
        {
            return client.DownloadString(url);
        }
    }

    private static void DownloadFile(string url, string destinationPath)
    {
        using (WebClient client = CreateWebClient())
        {
            client.DownloadFile(url, destinationPath);
        }
    }

    private static WebClient CreateWebClient()
    {
        WebClient client = new WebClient();
        client.Headers.Add(HttpRequestHeader.UserAgent, "UnityAmbientCGImporter/2.0");
        return client;
    }

    private static void CreateOrUpdateAllMaterials(
        string texturesFolder,
        string materialsFolder,
        Shader litShader,
        AmbientCGBulkImporter settings)
    {
        string texturesFolderAbs = ToAbsolutePath(texturesFolder);
        if (!Directory.Exists(texturesFolderAbs))
            return;

        string[] assetDirectories = Directory.GetDirectories(texturesFolderAbs, "*", SearchOption.TopDirectoryOnly);
        int total = assetDirectories.Length;

        for (int i = 0; i < total; i++)
        {
            string assetDirectoryAbs = assetDirectories[i];
            string assetId = new DirectoryInfo(assetDirectoryAbs).Name;

            EditorUtility.DisplayProgressBar(
                "ambientCG",
                "Criando materiais " + (i + 1) + "/" + total + ": " + assetId,
                0.92f + ((i + 1) / (float)Math.Max(1, total)) * 0.08f);

            try
            {
                MaterialBuildContext ctx = AnalyzeTextures(assetDirectoryAbs, assetId);
                if (ctx == null || !ctx.HasAnyTexture)
                    continue;

                ConfigureTextureImportSettings(ctx);

                string packedMetallicPath = null;
                if (!string.IsNullOrEmpty(ctx.MetallicPath) || !string.IsNullOrEmpty(ctx.RoughnessPath))
                {
                    packedMetallicPath = CreateMetallicSmoothnessTexture(ctx, assetDirectoryAbs, assetId);
                    if (!string.IsNullOrEmpty(packedMetallicPath))
                    {
                        AssetDatabase.ImportAsset(packedMetallicPath, ImportAssetOptions.ForceUpdate);
                        SetTextureImporterForLinearData(packedMetallicPath);
                    }
                }

                string materialPath = CombineAssetPath(materialsFolder, assetId + ".mat");
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null)
                {
                    material = new Material(litShader);
                    material.name = assetId;
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else if (!settings.overwriteExistingMaterials)
                {
                    continue;
                }
                else
                {
                    material.shader = litShader;
                }

                ApplyTexturesToMaterial(material, ctx, packedMetallicPath, settings);
                EditorUtility.SetDirty(material);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AmbientCG] Falha ao gerar material para " + assetId + ": " + ex.Message);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static MaterialBuildContext AnalyzeTextures(string assetDirectoryAbs, string assetId)
    {
        string[] files = Directory.GetFiles(assetDirectoryAbs, "*.*", SearchOption.AllDirectories)
            .Where(IsSupportedTextureFile)
            .ToArray();

        if (files.Length == 0)
            return null;

        MaterialBuildContext ctx = new MaterialBuildContext();
        ctx.AssetId = assetId;
        ctx.AssetDirectoryAbsolute = assetDirectoryAbs;

        for (int i = 0; i < files.Length; i++)
        {
            string fileAbs = files[i];
            string assetPath = ToAssetPath(fileAbs);
            string name = Path.GetFileName(fileAbs).ToLowerInvariant();

            if (IsProbablyPreview(name))
                continue;

            RegisterBestTexture(ref ctx.BaseColorPath, assetPath, ScoreBaseColor(name));
            RegisterBestTexture(ref ctx.NormalPath, assetPath, ScoreNormal(name));
            RegisterBestTexture(ref ctx.AmbientOcclusionPath, assetPath, ScoreAmbientOcclusion(name));
            RegisterBestTexture(ref ctx.RoughnessPath, assetPath, ScoreRoughness(name));
            RegisterBestTexture(ref ctx.MetallicPath, assetPath, ScoreMetallic(name));
            RegisterBestTexture(ref ctx.HeightPath, assetPath, ScoreHeight(name));
            RegisterBestTexture(ref ctx.EmissionPath, assetPath, ScoreEmission(name));
        }

        return ctx;
    }

    private static void ConfigureTextureImportSettings(MaterialBuildContext ctx)
    {
        SetTextureImporterForColor(ctx.BaseColorPath);
        SetTextureImporterForColor(ctx.EmissionPath);
        SetTextureImporterForNormal(ctx.NormalPath);
        SetTextureImporterForLinearData(ctx.AmbientOcclusionPath);
        SetTextureImporterForLinearData(ctx.RoughnessPath);
        SetTextureImporterForLinearData(ctx.MetallicPath);
        SetTextureImporterForLinearData(ctx.HeightPath);
    }

    private static void ApplyTexturesToMaterial(
        Material material,
        MaterialBuildContext ctx,
        string packedMetallicPath,
        AmbientCGBulkImporter settings)
    {
        material.shaderKeywords = new string[0];
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_WorkflowMode", 1f);
        material.SetFloat("_OcclusionStrength", settings.occlusionStrength);
        material.SetFloat("_Parallax", settings.parallaxStrength);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.5f);

        AssignTexture(material, "_BaseMap", ctx.BaseColorPath);
        AssignTexture(material, "_BumpMap", ctx.NormalPath, "_NORMALMAP");
        AssignTexture(material, "_OcclusionMap", ctx.AmbientOcclusionPath, "_OCCLUSIONMAP");
        AssignTexture(material, "_ParallaxMap", ctx.HeightPath, "_PARALLAXMAP");
        AssignTexture(material, "_EmissionMap", ctx.EmissionPath, "_EMISSION");

        if (!string.IsNullOrEmpty(ctx.EmissionPath))
            material.SetColor("_EmissionColor", Color.white);
        else
            material.SetColor("_EmissionColor", Color.black);

        if (!string.IsNullOrEmpty(packedMetallicPath))
        {
            AssignTexture(material, "_MetallicGlossMap", packedMetallicPath, "_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
        }
        else
        {
            material.SetTexture("_MetallicGlossMap", null);
        }
    }

    private static void AssignTexture(Material material, string propertyName, string texturePath, string keyword = null)
    {
        Texture texture = string.IsNullOrEmpty(texturePath) ? null : AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        material.SetTexture(propertyName, texture);

        if (texture != null && !string.IsNullOrEmpty(keyword))
            material.EnableKeyword(keyword);
        else if (!string.IsNullOrEmpty(keyword))
            material.DisableKeyword(keyword);
    }

    private static string CreateMetallicSmoothnessTexture(MaterialBuildContext ctx, string assetDirectoryAbs, string assetId)
    {
        Texture2D metallic = LoadTextureFromDisk(ctx.MetallicPath, true);
        Texture2D roughness = LoadTextureFromDisk(ctx.RoughnessPath, true);

        if (metallic == null && roughness == null)
            return null;

        int width = 4;
        int height = 4;

        if (metallic != null)
        {
            width = metallic.width;
            height = metallic.height;
        }
        else if (roughness != null)
        {
            width = roughness.width;
            height = roughness.height;
        }

        Texture2D packed = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float metal = SampleGray(metallic, x, y, width, height, 0f);
                float rough = SampleGray(roughness, x, y, width, height, 1f);
                float smooth = 1f - rough;
                pixels[y * width + x] = new Color(metal, 0f, 0f, smooth);
            }
        }

        packed.SetPixels(pixels);
        packed.Apply(false, false);

        string outputFileAbs = Path.Combine(assetDirectoryAbs, assetId + "_MetallicSmoothness.png");
        File.WriteAllBytes(outputFileAbs, packed.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(packed);
        if (metallic != null) UnityEngine.Object.DestroyImmediate(metallic);
        if (roughness != null) UnityEngine.Object.DestroyImmediate(roughness);

        return ToAssetPath(outputFileAbs);
    }

    private static float SampleGray(Texture2D texture, int x, int y, int targetWidth, int targetHeight, float fallback)
    {
        if (texture == null)
            return fallback;

        int sx = Mathf.Clamp(Mathf.RoundToInt((x / (float)Math.Max(1, targetWidth - 1)) * (texture.width - 1)), 0, texture.width - 1);
        int sy = Mathf.Clamp(Mathf.RoundToInt((y / (float)Math.Max(1, targetHeight - 1)) * (texture.height - 1)), 0, texture.height - 1);
        Color c = texture.GetPixel(sx, sy);
        return c.grayscale;
    }

    private static Texture2D LoadTextureFromDisk(string assetPath, bool linear)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        string absolute = ToAbsolutePath(assetPath);
        if (!File.Exists(absolute))
            return null;

        byte[] bytes = File.ReadAllBytes(absolute);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
        texture.LoadImage(bytes, false);
        return texture;
    }

    private static void ExtractZip(string zipPath, string destinationDirectory)
    {
        string destinationRoot = Path.GetFullPath(destinationDirectory) + Path.DirectorySeparatorChar;

        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
                if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                string entryDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(entryDirectory))
                    Directory.CreateDirectory(entryDirectory);

                entry.ExtractToFile(destinationPath, true);
            }
        }
    }

    private static bool IsSupportedTextureFile(string file)
    {
        string ext = Path.GetExtension(file).ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tif" || ext == ".tiff" || ext == ".tga" || ext == ".bmp";
    }

    private static bool IsProbablyPreview(string name)
    {
        return name.Contains("preview") || name.Contains("render") || name.Contains("sphere") || name.Contains("ball") || name.Contains("thumbnail");
    }

    private static int ScoreBaseColor(string name)
    {
        if (ContainsAny(name, "basecolor", "base_color", "albedo", "diffuse", "color"))
            return 100 + FormatBonus(name);
        return -1;
    }

    private static int ScoreNormal(string name)
    {
        if (ContainsAny(name, "normalgl", "normal_gl"))
            return 150 + FormatBonus(name);
        if (ContainsAny(name, "normaldx", "normal_dx"))
            return 140 + FormatBonus(name);
        if (ContainsAny(name, "normal"))
            return 120 + FormatBonus(name);
        return -1;
    }

    private static int ScoreAmbientOcclusion(string name)
    {
        if (ContainsAny(name, "ambientocclusion", "ambient_occlusion", "occlusion", "_ao", "ao_"))
            return 100 + FormatBonus(name);
        return -1;
    }

    private static int ScoreRoughness(string name)
    {
        if (ContainsAny(name, "roughness", "rough"))
            return 100 + FormatBonus(name);
        return -1;
    }

    private static int ScoreMetallic(string name)
    {
        if (ContainsAny(name, "metalness", "metallic"))
            return 100 + FormatBonus(name);
        return -1;
    }

    private static int ScoreHeight(string name)
    {
        if (ContainsAny(name, "displacement", "height", "disp", "parallax"))
            return 100 + FormatBonus(name);
        return -1;
    }

    private static int ScoreEmission(string name)
    {
        if (ContainsAny(name, "emission", "emissive"))
            return 100 + FormatBonus(name);
        return -1;
    }

    private static int FormatBonus(string name)
    {
        if (name.EndsWith(".png")) return 10;
        if (name.EndsWith(".tif") || name.EndsWith(".tiff")) return 8;
        if (name.EndsWith(".tga")) return 6;
        if (name.EndsWith(".jpg") || name.EndsWith(".jpeg")) return 4;
        return 0;
    }

    private static void RegisterBestTexture(ref ScoredPath slot, string assetPath, int score)
    {
        if (score < 0)
            return;

        if (slot == null || score > slot.Score)
            slot = new ScoredPath(assetPath, score);
    }

    private static void SetTextureImporterForColor(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;

        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }

        if (!importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void SetTextureImporterForLinearData(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;

        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }

        if (importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void SetTextureImporterForNormal(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;

        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            changed = true;
        }

        if (importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (text.Contains(values[i]))
                return true;
        }

        return false;
    }

    private static string NormalizeAssetFolder(string folder, string fallback)
    {
        string result = string.IsNullOrWhiteSpace(folder) ? fallback : folder.Trim().Replace('\\', '/');
        if (!result.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            result = "Assets/" + result.TrimStart('/');

        return result;
    }

    private static string CombineAssetPath(string left, string right)
    {
        return (left.TrimEnd('/') + "/" + right.TrimStart('/')).Replace('\\', '/');
    }

    private static void EnsureDirectoryAbsolute(string absolutePath)
    {
        if (!Directory.Exists(absolutePath))
            Directory.CreateDirectory(absolutePath);
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        string normalized = assetPath.Replace('\\', '/');

        if (normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(projectRoot, normalized).Replace('\\', '/');

        return normalized;
    }

    private static string ToAssetPath(string absolutePath)
    {
        string dataPath = Application.dataPath.Replace('\\', '/');
        string normalized = absolutePath.Replace('\\', '/');

        if (!normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O arquivo não está dentro da pasta Assets do projeto: " + absolutePath);

        return "Assets" + normalized.Substring(dataPath.Length);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private sealed class MaterialBuildContext
    {
        public string AssetId;
        public string AssetDirectoryAbsolute;

        public ScoredPath BaseColorPath;
        public ScoredPath NormalPath;
        public ScoredPath AmbientOcclusionPath;
        public ScoredPath RoughnessPath;
        public ScoredPath MetallicPath;
        public ScoredPath HeightPath;
        public ScoredPath EmissionPath;

        public bool HasAnyTexture
        {
            get
            {
                return BaseColorPath != null ||
                       NormalPath != null ||
                       AmbientOcclusionPath != null ||
                       RoughnessPath != null ||
                       MetallicPath != null ||
                       HeightPath != null ||
                       EmissionPath != null;
            }
        }
    }

    private sealed class ScoredPath
    {
        public string Path;
        public int Score;

        public ScoredPath(string path, int score)
        {
            Path = path;
            Score = score;
        }

        public static implicit operator string(ScoredPath value)
        {
            return value != null ? value.Path : null;
        }
    }
}
#endif