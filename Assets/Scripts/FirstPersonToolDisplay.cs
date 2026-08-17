using UnityEngine;

public class FirstPersonToolDisplay : MonoBehaviour
{
    public static FirstPersonToolDisplay Instance { get; private set; }

    [SerializeField] private Transform HandLocation;

    private GameObject currentModel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void EquipTool(ItemData tool)
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }

        if (tool == null || tool.gameModel == null)
            return;

        currentModel = Instantiate(tool.gameModel, HandLocation);
        currentModel.transform.localPosition = tool.modelPositionOffset;
        currentModel.transform.localRotation = Quaternion.Euler(tool.modelRotationOffset);
    }
}
