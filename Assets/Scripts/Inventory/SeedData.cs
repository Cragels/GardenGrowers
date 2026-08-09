using UnityEngine;

[CreateAssetMenu(menuName = "Items/Seed")]
public class SeedData : ItemData
{
    // Time it takes to grow
    public int daysToGrow;

    public ItemData cropToYield;
}
