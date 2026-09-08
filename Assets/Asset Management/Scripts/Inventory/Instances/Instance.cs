using UnityEngine;

public interface Instance
{
    AbstractItem itemData { get; }
    void Use();
}
