using UnityEngine;

public class PlotScript : MonoBehaviour
{
    public enum PlotStatus
    {
        Empty,
        Dug,
        Planted,
        Watered
    }

    public PlotStatus plotStatus;

    private void Start()
    {
        
    }

    public void SwitchPlotStatus(PlotStatus statusToSwitch)
    {
        plotStatus = statusToSwitch;

        switch (statusToSwitch)
        {
            case PlotStatus.Empty:
                //Should show empty plot and allows the player to interact while holding the shovel
                break;
            case PlotStatus.Dug:
                //Allows the player to now plant seeds and interact while holding seeds
                break;
            case PlotStatus.Planted:
                //player can now interact with a watering can and fertilizer
                break;
            case PlotStatus.Watered:
                //crop can now grow fire a watered bool to allow the plant to grow
                break;
        }
    }

    public void Interact()
    {
        ItemData toolSlot = InventoryManager.Instance.equippedTool;

        //Try casting the ite,Data in the toolSlot as EquipmentData
        EquipmentData equipmentTool = toolSlot as EquipmentData;

        //Check if it is of type equipmentData
        if (equipmentTool != null) 
        { 
            EquipmentData.ToolType toolType = equipmentTool.toolType;

            switch (toolType) 
            {
                case EquipmentData.ToolType.Shovel:
                    //Switches plot to dug status and allows seeds to be planted
                    break;
                case EquipmentData.ToolType.Seeds:
                    //switches status to planted seeds will require water and grow
                    break;
                case EquipmentData.ToolType.Fert:
                    //skips ahead the grow time of plant
                    break;
                case EquipmentData.ToolType.WateringCan:
                    //sets plants to watered and allows growth through a bool flag
                    break;

            }
        }
    }
}
