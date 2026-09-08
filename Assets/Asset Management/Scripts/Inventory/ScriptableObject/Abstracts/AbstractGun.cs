using UnityEditor.Animations;
using UnityEngine;

public abstract class AbstractGun : AbstractItem
{
    [Header("Gun property:")]
    [SerializeField]
    [Tooltip("ID da munição que a arma consome.")]
    private int ammoID;

    [SerializeField]
    [Tooltip("Quantidade máxima de balas que a arma comporta.")]
    private int bullets;

    public int getAmmoID { get => ammoID; }

    public int getMaxBullets => bullets;

    abstract public void shoot(GetGunAnimation gunAnimation);

    abstract public void reload(GetGunAnimation gunAnimation);
}