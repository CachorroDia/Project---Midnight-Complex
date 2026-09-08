using UnityEngine;

public class SlotsScript : MonoBehaviour
{
    [Tooltip("Imagem do slot, que é atualizada quando um item é carregado ou descarregado.")]
    [SerializeField]
    private GameObject slotImage;

    private byte itemTransparency;
    private Color slotColor;
    private byte slotTransparency;

    private Instance item;
    public void setItemTransparency(byte itemTransparency)
    {
        this.itemTransparency = itemTransparency;
    }

    public void setSlotColor(Color slotColor)
    {
        this.slotColor = slotColor;
    }

    public void setSlotTransparency(byte slotTransparency)
    {
        this.slotTransparency = slotTransparency;
    }

    public void Start()
    {
        this.GetComponent<UnityEngine.UI.Image>().color = new Color(slotColor.r, slotColor.g, slotColor.b, slotTransparency);
    }

    public void slotDisplay()
    {
        if (item == null)
        {
            slotImage.GetComponent<UnityEngine.UI.Image>().color = new Color32(255,255,255,0);
        }
        else
        {
            slotImage.GetComponent<UnityEngine.UI.Image>().sprite = item.itemData.getImage();
            slotImage.GetComponent<UnityEngine.UI.Image>().color = new Color32(255, 255, 255, itemTransparency);
        }
    }

    public Instance getItem() => item;

    public void setItem(Instance item)
    {
        this.item = item;
        slotDisplay();
    }

    public GameObject meshDisplay()
    {
        return item.itemData.getModel();
    }


}
