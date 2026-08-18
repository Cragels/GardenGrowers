using UnityEngine;

[CreateAssetMenu(menuName = "Items/Seed")]
public class SeedData : EquipmentData
{
    // Time it takes to grow
    public int daysToGrow;

    public ItemData cropToYield;

    public GameObject[] growthStagePrefabs;
}
