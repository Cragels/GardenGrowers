using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    public string description;

    //icon to be displayed in UI
    public Sprite thumbnail;

    //GameObject to be shown in the scene
    public GameObject gameModel;

    [Header("Model Alignment")]
    public Vector3 modelPositionOffset;
    public Vector3 modelRotationOffset;
}
