using UnityEngine;

public class ConversorMachine : MonoBehaviour
{
    [SerializeField]
    [Tooltip("GameObject do controlador de UI da máquina de conversão.")]
    private UIController uiController;

    public void addItems(AbstractScrap scrap)
    {
        uiController.addSynteticAlloy(scrap.getSynteticAlloy());
        uiController.addAlluminium(scrap.getAlluminium());
        uiController.addMechanicalParts(scrap.getMechanicalParts());
        uiController.addSteel(scrap.getSteel());
        uiController.addAuraVein(scrap.getAuraVein());
    }


}
