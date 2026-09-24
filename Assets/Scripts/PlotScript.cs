using UnityEngine;

public class PlotScript : MonoBehaviour, ITimeTracker
{
    public enum PlotStatus
    {
        Empty,
        Dug,
        Planted,
        Watered
    }

    public PlotStatus plotStatus;

    [Header("References")]
    [SerializeField] private Interactable interactable;
    [SerializeField] private Transform plantModelAnchor;
    [SerializeField] private GameObject dugVisual;
    [SerializeField] private GameObject emptyVisual;
    private GameObject currentPlantModel;

    private SeedData plantedSeed;
    private int growthStageIndex;
    private int lastProcessedDay;

    [Header("Messages")]
    [SerializeField] private string emptyMessage = "Dig with a Shovel";
    [SerializeField] private string dugMessage = "Plant Seeds";
    [SerializeField] private string plantedMessage = "Needs Watering";
    [SerializeField] private string wateredMessage = "Growing...";
    [SerializeField] private string readyMessage = "Ready to Harvest";

    GameTimeStamp timeWatered;
    GameTimeStamp timePlanted;

    [Header("Particles")]
    [SerializeField] private ParticleSystem dugParticles;
    [SerializeField] private ParticleSystem plantedParticles;
    [SerializeField] private ParticleSystem wateredParticles;
    [SerializeField] private ParticleSystem harvestParticles;



    private void Start()
    {
        TimeManager.Instance.RegisterTracker(this);
        interactable.messageProvider = GetCurrentMessage;
        RefreshVisuals();
    }

    private void OnEnable() => TimeManager.Instance?.RegisterTracker(this);
    private void OnDisable() => TimeManager.Instance?.UnregisterTracker(this);

    public void Interact()
    {
        if (IsFullyGrown())
        {
            Harvest();
            interactable.RefreshPrompt();
            return;
        }
        ItemData toolSlot = InventoryManager.Instance.equippedTool;
        EquipmentData equipmentTool = toolSlot as EquipmentData;
        if (equipmentTool == null) { interactable.RefreshPrompt(); return; }

        switch (plotStatus)
        {
            case PlotStatus.Empty:
                if (equipmentTool.toolType == EquipmentData.ToolType.Shovel)
                    SwitchPlotStatus(PlotStatus.Dug);
                break;

            case PlotStatus.Dug:
                if (equipmentTool.toolType == EquipmentData.ToolType.Seeds)
                {
                   

                    SeedData seed = toolSlot as SeedData;
                    if (seed == null)
                    {
                        Debug.LogWarning("Tool is marked ToolType.Seeds but isn't a SeedData asset: ");
                        break;
                    }

                    if (!InventoryManager.Instance.ConsumeEquippedTool()) break;

                    plantedSeed = seed;
                    
                    growthStageIndex = 0;
                    lastProcessedDay = GameTimeStamp.ToAbsoluteDay(TimeManager.Instance.GetGameTimeStamp());
                    SwitchPlotStatus(PlotStatus.Planted);
                }
                break;

            case PlotStatus.Planted:
                if (equipmentTool.toolType == EquipmentData.ToolType.WateringCan)
                    SwitchPlotStatus(PlotStatus.Watered);
                else if (equipmentTool.toolType == EquipmentData.ToolType.Fert)
                {
                    if (InventoryManager.Instance.ConsumeEquippedTool())
                        AdvanceGrowth();
                }
                    
                break;

            case PlotStatus.Watered:
                if (equipmentTool.toolType == EquipmentData.ToolType.Fert)
                {
                    if (InventoryManager.Instance.ConsumeEquippedTool())
                        AdvanceGrowth();
                }
                    
                break;
        }

        interactable.RefreshPrompt();
        /*
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
        */
    }
   
    private void AdvanceGrowth()
    {
        if (plantedSeed == null) return;
        growthStageIndex = Mathf.Min(growthStageIndex + 1, plantedSeed.growthStagePrefabs.Length - 1);
        RefreshVisuals();
    }
    public void SwitchPlotStatus(PlotStatus statusToSwitch)
    {
        plotStatus = statusToSwitch;
        if (statusToSwitch == PlotStatus.Empty)
        {
            plantedSeed = null;
            growthStageIndex = 0;
        }
        RefreshVisuals();
        PlayParticlesFor(statusToSwitch);
    }

    private void PlayParticlesFor(PlotStatus status)
    {
        ParticleSystem toPlay = status switch
        {
            PlotStatus.Dug => dugParticles,
            PlotStatus.Planted => plantedParticles,
            PlotStatus.Watered => wateredParticles,
            _ => null
        };
        toPlay?.Play();
    }

    private void Harvest()
    {
        InventoryManager.Instance.AddItem(plantedSeed.cropToYield, plantedSeed.cropToYield.addAmount, InventorySlot.InventoryType.Item);
        harvestParticles?.Play();
        SwitchPlotStatus(PlotStatus.Empty);
    }

    private void RefreshVisuals()
    {
        emptyVisual.SetActive(plotStatus == PlotStatus.Empty);
        dugVisual.SetActive(plotStatus == PlotStatus.Dug);

        if (currentPlantModel != null)
        {
            Destroy(currentPlantModel);
            currentPlantModel = null;
        }

        if ((plotStatus == PlotStatus.Planted || plotStatus == PlotStatus.Watered) && plantedSeed != null)
        {
            GameObject stagePrefab = plantedSeed.growthStagePrefabs[growthStageIndex];

            if (stagePrefab != null)
            {
                currentPlantModel = Instantiate(stagePrefab, plantModelAnchor);
                currentPlantModel.transform.localPosition = plantedSeed.plotModelPositionOffset;
                currentPlantModel.transform.localRotation = Quaternion.Euler(plantedSeed.plotModelRotationOffset);
            }
        }
        Debug.Log($"RefreshVisuals: status={plotStatus}, dugActive={dugVisual.activeSelf}, emptyActive={emptyVisual.activeSelf}");
    }

    private string GetCurrentMessage()
    {
        switch (plotStatus)
        {
            case PlotStatus.Empty: return emptyMessage;
            case PlotStatus.Dug: return dugMessage;
            case PlotStatus.Planted: return plantedMessage;
            case PlotStatus.Watered: return IsFullyGrown() ? readyMessage : wateredMessage;
            default: return "";
        }
    }
    private bool IsFullyGrown() =>
        plantedSeed != null && growthStageIndex >= plantedSeed.growthStagePrefabs.Length - 1;

    public void ClockUpdate(GameTimeStamp timeStamp)
    {
        /*
        if (plotStatus == PlotStatus.Watered) 
        {
           int hoursElapsed = GameTimeStamp.CompareTimestamps(timeWatered, timeStamp);
        }
        */
        if (plotStatus != PlotStatus.Planted && plotStatus != PlotStatus.Watered)
            return;

        int currentDay = GameTimeStamp.ToAbsoluteDay(timeStamp);
        if (currentDay == lastProcessedDay)
            return;

        lastProcessedDay = currentDay;

        if (plotStatus == PlotStatus.Watered)
            AdvanceGrowth(); // watered yesterday -> grows today

        if (!IsFullyGrown())
            plotStatus = PlotStatus.Planted; // watering lapses, needs re-watering

        interactable.RefreshPrompt();
        RefreshVisuals();
    }
}
