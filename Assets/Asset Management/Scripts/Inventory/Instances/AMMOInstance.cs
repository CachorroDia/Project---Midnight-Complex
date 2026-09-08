using UnityEngine;

public class AMMOInstance : Instance
{
    private AbstractAMMO ammoData;

    public AbstractItem itemData => ammoData;

    public AMMOInstance(AbstractAMMO ammoData)
    {
        this.ammoData = ammoData;
    }

    public void Use()
    {
        return;
    }

}
