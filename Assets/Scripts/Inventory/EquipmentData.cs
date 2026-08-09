using UnityEngine;

[CreateAssetMenu(menuName = "Items/Equipment")]
public class EquipmentData : ItemData
{
    public enum ToolType
    {
        Shovel, Seeds, Fert, WateringCan
    }
    public ToolType toolType;
}
