using UnityEngine;

public class GetGunAnimation : MonoBehaviour
{
    [SerializeField]
    [Tooltip("O animator do prefab que guarda as animações de Shoot e Reload da arma. (Os nomes dos triggers devem ser esses.)")]
    private Animator animator;


    private InventoryManager inv;
    public void SetInventoryManager(InventoryManager inventoryManager)
    {
        inv = inventoryManager;
    }


    public void PlayShoot()
    {
        if (animator != null)
            animator.SetTrigger("Shoot");
    }

    public void PlayReload()
    {
        if (animator != null)
            animator.SetTrigger("Reload");
    }

    public void OnShootAnimationFinished()
    {
        inv.UseFinished();
    }

}
