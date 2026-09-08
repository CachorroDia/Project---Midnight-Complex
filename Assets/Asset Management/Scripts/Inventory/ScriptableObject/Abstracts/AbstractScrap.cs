using UnityEngine;

public abstract class AbstractScrap : AbstractItem
{
    [Header("Scrap property:")]
    [SerializeField]
    [Tooltip("Quantidade de sucata sintética que o item pode ser convertido.")]
    private int synteticAlloy;

    [SerializeField]
    [Tooltip("Quantidade de sucata de alumínio que o item pode ser convertido.")]
    private int alluminium;

    [SerializeField]
    [Tooltip("Quantidade de sucata de aço que o item pode ser convertido.")]
    private int steel;

    [SerializeField]
    [Tooltip("Quantidade de sucata de peças mecânicas que o item pode ser convertido.")]
    private int mechanicalParts;

    [SerializeField]
    [Tooltip("Quantidade de sucata de AuraVein que o item pode ser convertido.")]
    private int auraVein;


    public int getSynteticAlloy() => synteticAlloy;
    public int getAlluminium() => alluminium;
    public int getSteel() => steel;
    public int getMechanicalParts() => mechanicalParts;
    public int getAuraVein() => auraVein;
}
