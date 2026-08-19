using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    public static ItemData Instance { get; set; }

    public string description;

    //icon to be displayed in UI
    public Sprite thumbnail;

    //GameObject to be shown in the scene
    public GameObject gameModel;

    [Header("Model Alignment")]
    public Vector3 modelPositionOffset;
    public Vector3 modelRotationOffset;

    [Header("Stacking")]
    public bool isStackable = true;
    public int addAmount = 1;
    public int maxStack = 30;

    [Header("Economy")]
    public int buyPrice;
    public int sellPrice;
}
