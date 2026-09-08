using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    [Header("Seed settings: ")]
    [SerializeField]
    [Tooltip("Seed de geração do mapa. Use o mesmo seed para gerar o mesmo mapa.")]
    private int seed;

    [Header("Generation settings: ")]
    [SerializeField]
    [Tooltip("Número máximo de salas a serem geradas.")]
    private int maxRooms;
    [SerializeField]
    [Tooltip("Número mínimo de salas a serem geradas.")]
    private int minRooms;
    [SerializeField]
    [Tooltip("Este número representa o quanto o mapa gerado irá concentrar em criar uma sequência de salas em um caminho principal ou se deve espalhar mais as salas. Quanto maior o número mais profundo é o labirinto. Atenção: Evite usar valores muito baixos!")]
    private int maxDepth;
    [SerializeField]
    [Tooltip("Limite no eixo Z que pode gerar salas.")]
    private int generationMaxHight;
    [SerializeField]
    [Tooltip("Limite no eixo X que pode gerar salas.")]
    private int generationMaxWidth;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Chance de criar conexões extras entre salas adjacentes. Quanto maior o número, mais conexões, por exemplo 0.25" +
        "corresponde a 25% de chance de criar uma conexão extra entre salas adjacentes.")]
    private float extraConnectionChance;
    [SerializeField]
    [Tooltip("Tamanho da grid fictícia do mapa. Atenção: O grid deve ser do tamanho da sala, valores ou pequenos demais podem causar problemas.")]
    private float gridSize;

    [Header("Itens spawn settings: ")]
    [SerializeField]
    [Tooltip("Número máximo de itens a serem gerados no mapa. O sistema irá sortear um valor entre o valor máximo e mínimo.")]
    private int maxItens;
    [SerializeField]
    [Tooltip("Número mínimo de itens a serem gerados no mapa. O sistema irá sortear um valor entre o valor máximo e mínimo.")]
    private int minItens;
    [SerializeField]
    [Tooltip("Prefabs dos objetos que irão spawnar. Todos os itens serão sorteados aleatoriamente.")]
    private List<GameObject> itensPrefabs;

    [Header("Unic prefabs:")]
    [SerializeField]
    [Tooltip("Prefab do jogador, onde o script irá criar e coloca-lo no começo do mundo.")]
    private GameObject playerPrefab;
    [SerializeField]
    [Tooltip("Prefab da sala do portal de volta, onde o script irá criar e coloca-lo em uma posição aleatória.")]
    private GameObject portalRoomPrefab;


    [Header("Rooms Prefabs: ")]
    [SerializeField]
    [Tooltip("Lista de prefabs das salas. O sistema irá escolher uma aleatoriamente para cada sala gerada, desde que esta sala cumpra o pré-requisito das conexões.")]
    private List<GameObject> roomsPrefabs;

    //Cria a classe de pseudo-random para gerar os números aleatórios a partir do seed
    private System.Random rng;

    //Matriz de salas geradas, onde cada posição representa uma sala
    private Room[,] rooms;

    //Número de salas a serem geradas pela seed, definido aleatoriamente entre o mínimo e o máximo
    private int roomsToGenerate;

    private int roomsGenerated;

    private readonly Vector2Int[] directionOffSet =
    {
        new Vector2Int(0, 1), //Norte
        new Vector2Int(0, -1), //Sul
        new Vector2Int(1, 0), //Leste
        new Vector2Int(-1, 0), //Oeste
    };

    private Dictionary<Direction, List<GameObject>> prefabByMask;

    public void Start()
    {
        Generate();
        Debug.Log("Salas geradas...");
    }

    private int Opossite(int dir)
    {
        if (dir == 0) return 1;
        if (dir == 1) return 0;
        if (dir == 2) return 3;
        return 2;
    }



    private void InitiateRNG()
    {
        rng = new System.Random(seed);
        roomsToGenerate = rng.Next(minRooms, maxRooms + 1);
    }

    private void Shuffle<T>(T[] directionList)
    {
        for (int i = directionList.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            T _temp = directionList[j];
            directionList[j] = directionList[i];
            directionList[i] = _temp;
        }
    }

    public void Generate()
    {
        InitiateRNG();
        OrganizePrefabList();
        StartGenerateGraph();
        AddExtrasConnections();
        SpawnRooms();
        //Spawn do portal tem que ficar aqui, antes dos itens!
        SpawnItems();
        //Falta spawnar: inimigos, colocar o portal de voltar.

    }

    private void StartGenerateGraph()
    {
        Room _room = new Room((new Vector2Int(Mathf.RoundToInt(generationMaxHight / 2), Mathf.RoundToInt(generationMaxWidth / 2))), 0);
        rooms = new Room[generationMaxHight, generationMaxWidth];
        rooms[_room.GetPosition().y, _room.GetPosition().x] = _room;
        CreatePlayer(_room);
        GenerateGraph(_room);
    }

    private void GenerateGraph(Room room)
    {
        room.SetVisited(true);
        int[] _directions = new int[4] { 0, 1, 2, 3 }; //Norte, Sul, Leste, Oeste   Respectivamente
        Shuffle<int>(_directions);

        foreach (int dir in _directions)
        {
            if (roomsGenerated >= roomsToGenerate) return;
            if (room.GetDepth() + 1 > maxDepth) return;

            Vector2Int _position = room.GetPosition() + directionOffSet[dir];

            //Verifica se a posição é válida para gerar uma sala
            if (rooms[_position.y, _position.x] != null) continue;

            //Verifica se a posição está dentro dos limites de geração
            if (((_position.x < 0) || (_position.x >= rooms.GetLength(1))) ||
                ((_position.y < 0) || (_position.y >= rooms.GetLength(0))))
                continue;

            Room _newRoom = new Room(_position, room.GetDepth() + 1);
            rooms[_position.y, _position.x] = _newRoom;
            room.SetRoom(dir, _newRoom);
            _newRoom.SetRoom(Opossite(dir), room);
            roomsGenerated++;

            GenerateGraph(_newRoom);
        }



    }

    //Organiza os prefabs das salas em um dicionário onde a chave é a máscara de conexões da sala e o valor é uma lista de prefabs
    //que possuem aquela máscara. Isso facilita a escolha aleatória de um prefab para cada sala gerada, garantindo que o prefab
    //escolhido cumpra o pré-requisito das conexões da sala.
    private void OrganizePrefabList()
    {
        prefabByMask = new Dictionary<Direction, List<GameObject>>();

        //Define apenas os bits válidos
        Direction validMask = Direction.North | Direction.South | Direction.East | Direction.West;

        foreach (GameObject prefab in roomsPrefabs)
        {
            Direction _mask = prefab.GetComponent<RoomPrefabData>().GetConnections();

            //Joga fora os bits inválidos, como por exemplo o everything da unity
            _mask &= validMask;

            if (!prefabByMask.ContainsKey(_mask))
            {
                prefabByMask[_mask] = new List<GameObject>();
            }

            prefabByMask[_mask].Add(prefab);
        }
    }

    private void AddExtrasConnections()
    {
        for (int y = 0; y < generationMaxHight; y++)
        {
            for (int x = 0; x < generationMaxWidth; x++)
            {
                if (rooms[y, x] != null)
                {
                    AddExtraConnection(rooms[y, x]);
                }
            }
        }
    }
    private void AddExtraConnection(Room room)
    {
        for (int i = 0; i < room.GetRooms().Length; i++)
        {
            if (room.GetRoom(i) != null) continue;

            Vector2Int neighborPos = room.GetPosition() + directionOffSet[i];

            if (neighborPos.x < 0 || neighborPos.x >= generationMaxWidth ||
                neighborPos.y < 0 || neighborPos.y >= generationMaxHight)
                continue;

            if (rooms[(neighborPos).y, (neighborPos).x] == null) continue;

            if (rng.NextDouble() > extraConnectionChance) continue;

            room.SetRoom(i, rooms[(neighborPos).y, (neighborPos).x]);
            rooms[(neighborPos).y, (neighborPos).x].SetRoom(Opossite(i), room);
        }
    }

    private void SpawnRooms()
    {
        for (int y = 0; y < generationMaxHight; y++)
        {
            for (int x = 0; x < generationMaxWidth; x++)
            {
                Room room = rooms[y, x];
                if (room == null) continue;

                SpawnRoom(room);
            }
        }
    }
    private void SpawnRoom(Room room)
    {
        Direction _mask = room.GetConnections();

        if (!prefabByMask.ContainsKey(_mask))
        {
            Debug.LogError("Não existe prefab para a máscara de conexões: " + _mask);
            return;
        }

        GameObject _chosenPrefab = prefabByMask[_mask][rng.Next(0, prefabByMask[_mask].Count)];

        Vector3 _worldPosition = new Vector3(room.GetPosition().x * gridSize, 0, room.GetPosition().y * gridSize);

        //Essa linha guarda onde itens ou criaturas podem spawnar.
        room.SetSpawnPositions(_chosenPrefab.GetComponent<RoomPrefabData>());

        Instantiate(_chosenPrefab, _worldPosition, Quaternion.identity);
    }

    private void SpawnItems()
    {
        int _itemsAmount = rng.Next(minItens, maxItens + 1);

        List<(Room room, int _spawnIndex)> _validSpawns = new List<(Room room, int _spawnIndex)>();

        for (int y = 0; y < generationMaxHight; y++)
        {
            for (int x = 0; x < generationMaxWidth; x++)
            {
                if (rooms[y, x] == null) continue;

                GameObject[] _spawns = rooms[y, x].GetSpawnPositions();
                if(_spawns == null) continue;

                for (int i = 0; i < _spawns.Length; i++)
                {
                    if (_spawns[i] != null)
                        _validSpawns.Add((rooms[y, x], i));
                }
            }
        }

        if (_validSpawns.Count == 0) return;

        _itemsAmount = Mathf.Min(_itemsAmount, _validSpawns.Count);

        for (int i = 0; i < _itemsAmount; i++)
        {
            int index = rng.Next(0, _validSpawns.Count);

            (Room room, int _spawnIndex) chosen = _validSpawns[index];

            GameObject _item = itensPrefabs[rng.Next(itensPrefabs.Count)];

            Instantiate(_item, chosen.room.GetSpawnPositions()[chosen._spawnIndex].GetComponent<Transform>().position, Quaternion.identity);

            _validSpawns.RemoveAt(index);
        }



    }

    private void CreatePlayer(Room room)
    {
        Instantiate(playerPrefab, new Vector3(room.GetPosition().x * gridSize, 0, room.GetPosition().y * gridSize), Quaternion.identity);
    }


}

    [System.Flags]
    public enum Direction
    {
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }