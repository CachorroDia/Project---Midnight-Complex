using UnityEngine;

public class UIController : MonoBehaviour
{
    private int synteticAlloy;
    private int alluminium;
    private int mechanicalParts;
    private int steel;
    private int money;
    private int auraVein;


    [Header("UI information slots:")]
    [SerializeField]
    [Tooltip("GameObject do slot de Syntetic Alloy.")]
    private GameObject synteticAlloySlot;

    [SerializeField]
    [Tooltip("GameObject do slot de Alluminium.")]
    private GameObject alluminiumSlot;

    [SerializeField]
    [Tooltip("GameObject do slot de Mechanical Parts.")]
    private GameObject mechanicalPartsSlot;

    [SerializeField]
    [Tooltip("GameObject do slot de Steel.")]
    private GameObject steelSlot;

    [SerializeField]
    [Tooltip("GameObject do slot de Dinheiro.")]
    private GameObject moneySlot;

    [SerializeField]
    [Tooltip("GameObject do slot de Aura Vein.")]
    private GameObject auraVeinSlot;

    [Header("UI Information:")]
    [SerializeField]
    [Tooltip("Texto de display de Syntetic Alloy.")]
    private string synteticAlloyText;

    [SerializeField]
    [Tooltip("Texto de display de Alluminium.")]
    private string alluminiumText;

    [SerializeField]
    [Tooltip("Texto de display de Mechanical Parts.")]
    private string mechanicalPartsText;

    [SerializeField]
    [Tooltip("Texto de display de Mechanical Parts.")]
    private string steelText;

    [SerializeField]
    [Tooltip("Texto de display do Dinheiro.")]
    private string moneyText;

    [SerializeField]
    [Tooltip("Texto de display do Aura Vein.")]
    private string auraVeinText;


    public void saveUIData()
    {
        UIinformationData _data = new UIinformationData();
        _data.synteticAlloy = synteticAlloy;
        _data.alluminium = alluminium;
        _data.mechanicalParts = mechanicalParts;
        _data.steel = steel;
        _data.money = money;
        _data.auraVein = auraVein;

        string _dataJson = JsonUtility.ToJson(_data);
        string _path = Application.persistentDataPath + "/UIData.json";
        Debug.Log("Saving inventory data to: " + _path);
        System.IO.File.WriteAllText(_path, _dataJson);
        Debug.Log("Inventory data saved!");
    }


    public void loadUIData()
    {
        string _path = Application.persistentDataPath + "/UIData.json";
        string _inventoryDataJson = System.IO.File.ReadAllText(_path);

        UIinformationData _data = new UIinformationData();
        _data = JsonUtility.FromJson<UIinformationData>(_inventoryDataJson);
        this.synteticAlloy = _data.synteticAlloy;
        this.alluminium = _data.alluminium;
        this.mechanicalParts = _data.mechanicalParts;
        this.steel = _data.steel;
        this.money = _data.money;
        this.auraVein = _data.auraVein;
    }


    public void displayInformation()
    {
        synteticAlloySlot.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = synteticAlloyText + synteticAlloy;
        alluminiumSlot.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = alluminiumText + alluminium;
        mechanicalPartsSlot.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = mechanicalPartsText + mechanicalParts;
        steelSlot.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = steelText + steel;
        moneySlot.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = moneyText + money;
        auraVeinSlot.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = auraVeinText + auraVein;
    }


    public void Start()
    {
        string _path = Application.persistentDataPath + "/UIData.json";
        if (System.IO.File.Exists(_path) )
        {
            loadUIData();
        }
        displayInformation();
    }

    /*
    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            saveUIData();
        }
    }
    */ //Teste de salvar os dados do inventário ao pressionar a tecla K.

    public void addSynteticAlloy(int amount)
    {
        synteticAlloy += amount;
        displayInformation();
    }

    public void addAlluminium(int amount)
    {
        alluminium += amount;
        displayInformation();
    }

    public void addMechanicalParts(int amount)
    {
        mechanicalParts += amount;
        displayInformation();
    }

    public void addSteel(int amount)
    {
        steel += amount;
        displayInformation();
    }

    public void addMoney(int amount)
    {
        money += amount;
        displayInformation();
    }

    public void addAuraVein(int amount)
    {
        auraVein += amount;
        displayInformation();
    }

    public int getSynteticAlloy() => synteticAlloy;
    public int getAlluminium() => alluminium;
    public int getMechanicalParts() => mechanicalParts;
    public int getSteel() => steel;
    public int getMoney() => money;
    public int getAuraVein() => auraVein;
}


//Dados do inventário
[System.Serializable]
public class UIinformationData
{
    public int synteticAlloy;
    public int alluminium;
    public int mechanicalParts;
    public int steel;
    public int money;
    public int auraVein;
}