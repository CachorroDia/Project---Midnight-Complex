using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private List<SlotsScript> slots;
    private SlotsScript slot;
    private int slotIndex;
    private GameObject handItem;

    [Header("Attachs obrigatórios:")]
    [SerializeField]
    [Tooltip("Câmera da visão do jogador.")]
    private GameObject cameraPlayer;

    [SerializeField]
    [Tooltip("Objeto vazio posicionado na mão do jogador, quando selecionado o modelo 3D será renderizado nessa posição.")]
    private GameObject hand;

    [Header("Configurações gerais:")]
    [SerializeField]
    [Tooltip("Distância máxima onde o jogador alcança para pegar/soltar itens.")]
    private float rayCastDistance;

    [SerializeField]
    [Tooltip("O quanto os itens droppados pelo jogador ficam acima do chão, ou seja, um offset para cima.")]
    private float droppedItemOffset;

    [SerializeField]
    [Tooltip("Layer onde o jogo permite colocar itens, como chão ou plataformas.")]
    private string placebleItemTag;

    [SerializeField]
    [Tooltip("Layer onde os itens dropados ficam.")]
    private string droppedItemLayer;

    [SerializeField]
    [Tooltip("HUD de onde vai mosrar a quantidade de balas restantes.")]
    private GameObject textAMMOLeft;

    [Header("Configurações de controle:")]
    [SerializeField]
    [Tooltip("Tecla para soltar o item selecionado.")]
    private KeyCode dropItemKey;

    [SerializeField]
    [Tooltip("Tecla para pegar o item.")]
    private KeyCode pickItemKey;

    [SerializeField]
    [Tooltip("Tecla para recarregar a arma.")]
    private KeyCode reloadGun;

    [SerializeField] private AbstractGun armateste; //Somente para Debug
    [SerializeField] private AbstractScrap sucatateste; //Somente para Debug
    [SerializeField] private AbstractAMMO ammoteste; //Somente para Debug

    [Header("Visual Settings:")]
    [SerializeField]
    [Tooltip("Cor de destaque para o slot selecionado.")]
    private Color selectColor;

    [SerializeField]
    [Tooltip("Cor de fundo para os não selecionados")]
    private Color slotColor;

    [SerializeField]
    [Range(0f, 255f)]
    [Tooltip("Define a transparência dos itens quando são carregados nos slots.")]
    private byte itemTransparency;

    [SerializeField]
    [Range(0f, 255f)]
    [Tooltip("Define a transparência dos slots.")]
    private byte slotTransparency;

    //Trava para evitar que o jogador faça ações enquanto usa um item
    private bool lockInventory = false;


    public void Awake()
    {
        slots = new List<SlotsScript>( GetComponentsInChildren<SlotsScript>() );

        foreach(SlotsScript _slot in slots)
        {
            _slot.setItemTransparency(itemTransparency);
            _slot.setSlotColor(slotColor);
            _slot.setSlotTransparency(slotTransparency);
            _slot.slotDisplay();
        }
    }

    IEnumerator Start()
    {
        yield return null; // espera 1 frame
        selectSlot(0);
    }



    public void Update()
    {
        KeyCode[] numberKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
            KeyCode.Alpha0
        };

        if (!lockInventory)
        {
            for (int i = 0; i < slots.Count && i < numberKeys.Length; i++)
            {
                if (Input.GetKeyDown(numberKeys[i]))
                {
                    slotIndex = i;
                    selectSlot(i);
                }
            }


            //Mouse Scroll
            float _scroll = Input.GetAxis("Mouse ScrollWheel");
            //Scroll pra cima
            if (_scroll > 0f)
            {
                if (slotIndex + 1 >= slots.Count)
                {
                    slotIndex = 0;
                }
                else
                {
                    slotIndex++;
                }
                selectSlot(slotIndex);
            }
            //Scroll pra baixo
            if (_scroll < 0f)
            {
                if (slotIndex - 1 < 0)
                {
                    slotIndex = slots.Count - 1;
                }
                else
                {
                    slotIndex--;
                }
                selectSlot(slotIndex);
            }

            //Usar item
            if (Input.GetMouseButtonDown(0))
            {
                if (slot.getItem() != null)
                {
                    Use();
                }
            }

            //Recarregar arma
            if (Input.GetKeyDown(reloadGun))
            {
                if (slot.getItem() is GunInstance)
                {
                    checkAMMO();
                }
            }


            //Soltar item
            if (Input.GetKeyDown(dropItemKey))
            {
                dropItens();
            }

            //Pegar item
            if (Input.GetKeyDown(pickItemKey))
            {
                pickItem();
            }
        }


        
        //Debug para salvar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveData();
        }
        //Debug para gerar uma arma
        if(Input.GetKeyDown(KeyCode.G))
        {
            GunInstance armaTeste = new GunInstance(armateste, 3);  //Precisa de um serialized field
            slots[slotIndex].setItem(armaTeste);
            selectSlot(slotIndex);
        }
        //Debug para gerar uma sucata
        if (Input.GetKeyDown(KeyCode.H))
        {
            ScrapInstance sucataTeste = new ScrapInstance(sucatateste); //Precisa de um serialized field
            slots[slotIndex].setItem(sucataTeste);
            selectSlot(slotIndex);
        }
        //Debug para gerar uma munição
        if (Input.GetKeyDown(KeyCode.J))
        {
            AMMOInstance ammoTeste = new AMMOInstance(ammoteste); //Precisa de um serialized field
            slots[slotIndex].setItem(ammoTeste);
            selectSlot(slotIndex);
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            string _path = Application.persistentDataPath + "/inventoryFiles.json";
            if (File.Exists(_path))
            {
                File.Delete(_path);
                Debug.Log("Inventory Data deleted: " + _path);
            }
            else
            {
                Debug.Log("Nenhum save encontrado.");
            }
        }
        //Debug para salvar e gerar itens
        

    }

    public void selectSlot(int selectedSlot)
    {
        if(slot != null)
        {
            slot.GetComponent<UnityEngine.UI.Image>().color = slotColor;
        }
        if(handItem != null)
        {
            Destroy(handItem);
        }
        slot = slots[selectedSlot];
        slot.GetComponent<UnityEngine.UI.Image>().color = selectColor;
        loadMesh();

        updateBulletDisplay();
    }

    private void updateBulletDisplay()
    {
        if (slot.getItem() is GunInstance gun)
        {
            textAMMOLeft.SetActive(true);
            textAMMOLeft.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = gun.GetCurrentBullets().ToString() +
                " / " + gun.GetMaxBullets();
        }
        else
        {
            textAMMOLeft.SetActive(false);
        }
    }

    private void loadMesh()
    {
        if (slot.getItem() == null) return;

        GameObject _item = Instantiate(slot.getItem().itemData.getModel(), hand.transform);
        handItem = _item;

        //Vincular a animação da arma, caso seja uma arma
        connectAnimation();
    }

    private void connectAnimation()
    {
        if (slot.getItem() is GunInstance gun)
        {
            gun.setGunAnimation(handItem.GetComponent<GetGunAnimation>());

            handItem.GetComponent<GetGunAnimation>().SetInventoryManager(this);
        }
    }

    public void SaveData()
    {
        DataWrapper dataWrapper = new DataWrapper();

        foreach (SlotsScript _slot in slots)
        {
            if(_slot.getItem() != null)
            {
                //Itens do tipo arma
                if(_slot.getItem() is GunInstance gun)
                {
                    ItemData weaponData = new ItemData();
                    weaponData.bullets = gun.GetCurrentBullets();
                    weaponData.ID = gun.itemData.getID();
                    weaponData.type = ItemType.Gun;
                    dataWrapper._items.Add(weaponData);
                }
                //Itens do tipo sucata
                if (_slot.getItem() is ScrapInstance scrap)
                {
                    ItemData scrapData = new ItemData();
                    scrapData.ID = scrap.itemData.getID();
                    scrapData.type = ItemType.Scrap;
                    dataWrapper._items.Add(scrapData);
                }
                //Itens do tipo munição
                if (_slot.getItem() is AMMOInstance ammo)
                {
                    ItemData ammoData = new ItemData();
                    ammoData.ID = ammo.itemData.getID();
                    ammoData.type = ItemType.AMMO;
                    dataWrapper._items.Add(ammoData);
                }
            }
            else
            {
                ItemData itemData = new ItemData();
                itemData.ID = -1;
                dataWrapper._items.Add(itemData);
            }
        }
        string data = JsonUtility.ToJson(dataWrapper);
        string path = Application.persistentDataPath + "/inventoryFiles.json";
        Debug.Log("Saving data to: " + path);
        System.IO.File.WriteAllText(path, data);
        Debug.Log("Data saved");
    }

    public List<SlotsScript> getSlots() => slots;

    public SlotsScript getSlot(int index) => slots[index];

    private void dropItens()
    {
        if (slot.getItem() != null)
        {
            Ray ray = cameraPlayer.GetComponent<Camera>().ScreenPointToRay(new Vector3((Screen.width / 2), (Screen.height / 2), 0));
            Vector3 hitPoint;

            int _layerMask = LayerMask.GetMask(placebleItemTag);

            if (Physics.Raycast(ray, out RaycastHit hit, rayCastDistance, _layerMask))
            {
                //Verificar se é a máquina conversora e se está segurando um scrap
                if((hit.collider.TryGetComponent<ConversorMachine>(out ConversorMachine machine)) && (slot.getItem() is ScrapInstance))
                {
                    machine.addItems((AbstractScrap)slot.getItem().itemData);
                    slot.setItem(null);
                    return;
                }

                //Se não for a máquina, droppa o item normalmente
                hitPoint = hit.point;
                hitPoint += Vector3.up * droppedItemOffset;
                GameObject _item = Instantiate(slot.getItem().itemData.getModel(), hitPoint, Quaternion.Euler(0f, cameraPlayer.transform.eulerAngles.y, 0f) );
                DroppedItems droppedItem = _item.AddComponent<DroppedItems>();
                droppedItem.setItem(slot.getItem());
                _item.layer = LayerMask.NameToLayer(droppedItemLayer);
                slot = null;
                slots[slotIndex].setItem(null);
                selectSlot(slotIndex);
            }
        }
    }

    private void pickItem()
    {
        if(slot.getItem() == null)
        {
            Ray ray = cameraPlayer.GetComponent<Camera>().ScreenPointToRay(new Vector3((Screen.width / 2), (Screen.height / 2), 0));

            int _layerMask = LayerMask.GetMask(droppedItemLayer);

            if (Physics.Raycast(ray, out RaycastHit hit, rayCastDistance, _layerMask))
            {
                if(hit.collider.gameObject.TryGetComponent<DroppedItems>(out DroppedItems droppedItem))
                {
                    slot.setItem(droppedItem.getInstance());
                    Destroy(hit.collider.gameObject);
                    selectSlot(slotIndex);
                }
            }

        }
    }

    private void Use()
    {
        lockInventory = true;
        slot.getItem().Use();
        updateBulletDisplay();
    }

    public void UseFinished()
    {
        lockInventory = false;
    }

    public void ReloadFinished()
    {
        updateBulletDisplay();
        lockInventory = false;
    }

    private void checkAMMO()
    {
        GunInstance _gun = slot.getItem() as GunInstance;
        AbstractGun _gunData = _gun.itemData as AbstractGun;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].getItem() is AMMOInstance ammo)
            {
                if(_gunData.getAmmoID == ammo.itemData.getID())
                {
                    lockInventory = true;
                    slots[i].setItem(null);
                    _gun.reload();
                    break;
                }
                else
                {
                    UseFinished();
                }
            }
        }
    }


}
