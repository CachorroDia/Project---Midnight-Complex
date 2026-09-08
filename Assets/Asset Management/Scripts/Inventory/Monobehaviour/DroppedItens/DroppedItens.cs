using UnityEngine;

public class DroppedItems : MonoBehaviour
{
    private Instance instance;

    public void setItem(Instance instance)
    {
        this.instance = instance;
    }

    public Instance getInstance() => instance;


}
