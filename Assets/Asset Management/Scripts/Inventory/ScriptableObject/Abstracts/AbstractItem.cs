using UnityEngine;

public abstract class AbstractItem : ScriptableObject
{
    [Header("Display:")]
    [SerializeField]
    [Tooltip("Imagem que aparecerá nos slots do inventário (precisa ser do tipo sprite).")]
    private Sprite image;

    [SerializeField]
    [Tooltip("Modelo 3D do item, usado para mostrar o item no mundo e na mão do personagem.")]
    private GameObject model;

    [Header("Item info:")]
    [SerializeField]
    [Tooltip("ID único do item, usado para identificá-lo no código e no inventário.")]
    private int ID;

    [SerializeField]
    [Tooltip("Tipo do item, usado para categorizar o item e determinar seu comportamento no jogo. Gun o item é tratado como arma, Scrap o item é tratado como um item que pode ser convertido em recursos e AMMO o item é considerado um cartucho para uma das armas")]
    private ItemType type;

    public Sprite getImage() => image;

    public GameObject getModel() => model;

    public int getID() => ID;

    public ItemType getType() => type;
    
}
