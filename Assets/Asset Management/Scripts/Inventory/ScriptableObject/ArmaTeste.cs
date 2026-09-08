using UnityEngine;

[CreateAssetMenu(fileName = "ArmaDeTeste", menuName = "Weapons/ArmaDeTeste", order = 1)]
public class ArmaTeste : AbstractGun
{
    public override void reload(GetGunAnimation gunAnimation)
    {
        Debug.Log("Recarregou a arma de teste!");
    }

    public override void shoot(GetGunAnimation gunAnimation)
    {
        Debug.Log("Atirou com a arma de teste!");
        gunAnimation.PlayShoot();
    }

}