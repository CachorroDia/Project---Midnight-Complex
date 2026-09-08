using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class InventoryLoader : MonoBehaviour
{
    private InventoryManager inventoryManager;

    [SerializeField]
    [Tooltip("Lista de itens disponíveis no jogo, para o sistema de salvamento funcionar, é necessário que os itens estejam nessa lista.")]
    private List<AbstractItem> item;

    //Lista que recebe a informação de todo inventário
    private DataWrapper data = new DataWrapper();


    private void Start()
    {
        inventoryManager = this.GetComponent<InventoryManager>();

        string path = Application.persistentDataPath + "/inventoryFiles.json";
        if (System.IO.File.Exists(path))
        {

            loadJson();

            for (int i = 0; i < data._items.Count; i++)
            {
                if (data._items[i].ID > -1)
                {
                    for (int j = 0; j < item.Count; j++)
                    {
                        if (data._items[i].ID == item[j].getID())
                        {
                            if (data._items[i].type == ItemType.Gun)
                            {
                                GunInstance gun = new GunInstance((AbstractGun)item[j], data._items[i].bullets);
                                inventoryManager.getSlots()[i].setItem(gun);
                            }
                            
                            if (data._items[i].type == ItemType.Scrap)
                            {
                                ScrapInstance scrap = new ScrapInstance((AbstractScrap)item[j]);
                                inventoryManager.getSlots()[i].setItem(scrap);
                            }

                            if (data._items[i].type == ItemType.AMMO)
                            {
                                AMMOInstance ammo = new AMMOInstance((AbstractAMMO)item[j]);
                                inventoryManager.getSlots()[i].setItem(ammo);
                            }

                        }
                    }
                }
            }
        }
        inventoryManager.selectSlot(0);
        Destroy(this);
    }


    private void loadJson()
    {
        string path = Application.persistentDataPath + "/inventoryFiles.json";
        string inventoryData = System.IO.File.ReadAllText(path);

        data = JsonUtility.FromJson<DataWrapper>(inventoryData);
    }


}





[System.Serializable]
public class DataWrapper
{
    public List<ItemData> _items = new List<ItemData>();
}

public enum ItemType
{
    Gun,
    Scrap,
    AMMO
}

[System.Serializable]
public class ItemData
{
    public int ID;
    public int bullets;
    public ItemType type;
}