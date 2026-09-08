using UnityEngine;

public class GunInstance : Instance
{
    private int bullets;
    private AbstractGun gunData;

    public AbstractItem itemData => gunData;

    public GunInstance(AbstractGun gunData, int bullets)
    {
        this.gunData = gunData;
        this.bullets = bullets;
    }

    public int GetCurrentBullets() => bullets;

    public int GetMaxBullets() => gunData.getMaxBullets;



    private GetGunAnimation gunAnimation;
    public void setGunAnimation(GetGunAnimation gunAnimation)
    {
        this.gunAnimation = gunAnimation;
    }

    public void Use()
    {
        if(bullets > 0)
        {
            gunData.shoot(gunAnimation);
            bullets -= 1;
        }
        else
        {
            gunAnimation.OnShootAnimationFinished();
        }
    }

    public void reload()
    {
        gunData.reload(gunAnimation);
    }
}
