using UnityEngine;
using UnityEngine.Events;

public class SpecialPlotScript : MonoBehaviour, ITimeTracker
{
    public enum PlotStatus
    {
        Empty,
        Dug,
        Planted,
        Watered
    }

    public PlotStatus plotStatus;

    [Header("Events")]
    public UnityEvent onFullyGrown;

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
    [SerializeField] private string dugMessage = "Plant a Special Seed";
    [SerializeField] private string plantedMessage = "Needs Watering";
    [SerializeField] private string wateredMessage = "Growing...";
    [SerializeField] private string fullyGrownMessage = "Fully Grown"; // cannot be harvested

    GameTimeStamp timeWatered;
    GameTimeStamp timePlanted;

    [Header("Particles")]
    [SerializeField] private ParticleSystem dugParticles;
    [SerializeField] private ParticleSystem plantedParticles;
    [SerializeField] private ParticleSystem wateredParticles;
    [SerializeField] private ParticleSystem weedParticles;

    [Header("Weeds")]
    [SerializeField] private GameObject weedVisual;
    [SerializeField, Range(0f, 1f)] private float weedChancePerDay = 1f;
    [SerializeField] private string weedsMessage = "Pull the Weeds";

    private bool hasWeeds;


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
        // Fully grown special plants can't be harvested, so there's nothing to do
        if (IsFullyGrown())
        {
            interactable.RefreshPrompt();
            return;
        }

        if (hasWeeds)
        {
            RemoveWeeds();
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
                {
                    SwitchPlotStatus(PlotStatus.Dug);
                    SFXManager.Instance.PlaySound("Dig");
                }
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

                    // Only special seeds can be planted in this plot
                    if (!seed.IsSpecialSeed)
                    {
                        Debug.Log("This plot only accepts special seeds.");
                        break;
                    }

                    if (!InventoryManager.Instance.ConsumeEquippedTool()) break;

                    plantedSeed = seed;

                    growthStageIndex = 0;
                    lastProcessedDay = GameTimeStamp.ToAbsoluteDay(TimeManager.Instance.GetGameTimeStamp());
                    SwitchPlotStatus(PlotStatus.Planted);
                    SFXManager.Instance.PlaySound("Plant");
                }
                break;

            case PlotStatus.Planted:
                if (equipmentTool.toolType == EquipmentData.ToolType.WateringCan)
                {
                    SwitchPlotStatus(PlotStatus.Watered);
                    SFXManager.Instance.PlaySound("Water");
                }
                // Fert does nothing here: plot must be watered first
                break;

            case PlotStatus.Watered:
                if (equipmentTool.toolType == EquipmentData.ToolType.Fert)
                {
                    if (InventoryManager.Instance.ConsumeEquippedTool())
                    {
                        AdvanceGrowth();
                        TryGrowWeeds();
                    }
                }
                break;
        }

        interactable.RefreshPrompt();
    }

    private void AdvanceGrowth()
    {
        if (plantedSeed == null) return;

        bool wasFullyGrown = IsFullyGrown();

        growthStageIndex = Mathf.Min(growthStageIndex + 1, plantedSeed.growthStagePrefabs.Length - 1);
        RefreshVisuals();

        if (!wasFullyGrown && IsFullyGrown())
            onFullyGrown?.Invoke();
    }

    public void SwitchPlotStatus(PlotStatus statusToSwitch)
    {
        plotStatus = statusToSwitch;
        if (statusToSwitch == PlotStatus.Empty)
        {
            plantedSeed = null;
            growthStageIndex = 0;
            hasWeeds = false;
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

    public void OnPlantEaten()
    {
        if (plotStatus != PlotStatus.Planted && plotStatus != PlotStatus.Watered)
            return;

        SwitchPlotStatus(PlotStatus.Empty);
    }

    private void RefreshVisuals()
    {
        emptyVisual.SetActive(plotStatus == PlotStatus.Empty);
        dugVisual.SetActive(plotStatus == PlotStatus.Dug);

        if (weedVisual != null)
            weedVisual.SetActive(hasWeeds &&
                (plotStatus == PlotStatus.Planted || plotStatus == PlotStatus.Watered));

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
    }

    private string GetCurrentMessage()
    {
        if (IsFullyGrown()) return fullyGrownMessage;
        if (hasWeeds) return weedsMessage;
        switch (plotStatus)
        {
            case PlotStatus.Empty: return emptyMessage;
            case PlotStatus.Dug: return dugMessage;
            case PlotStatus.Planted: return WithDaysLeft(plantedMessage);
            case PlotStatus.Watered: return WithDaysLeft(wateredMessage);
            default: return "";
        }
    }

    // Each growth stage takes one watered day, so days left = stages remaining
    private int DaysUntilFullyGrown() =>
        plantedSeed == null ? 0 : Mathf.Max(0, plantedSeed.growthStagePrefabs.Length - 1 - growthStageIndex);

    private string WithDaysLeft(string message)
    {
        int days = DaysUntilFullyGrown();
        return $"{message} ({days} {(days == 1 ? "day" : "days")} left)";
    }

    private bool IsFullyGrown() =>
        plantedSeed != null && growthStageIndex >= plantedSeed.growthStagePrefabs.Length - 1;

    public void ClockUpdate(GameTimeStamp timeStamp)
    {
        if (plotStatus != PlotStatus.Planted && plotStatus != PlotStatus.Watered)
            return;

        int currentDay = GameTimeStamp.ToAbsoluteDay(timeStamp);
        if (currentDay == lastProcessedDay)
            return;

        lastProcessedDay = currentDay;

        if (plotStatus == PlotStatus.Watered && !hasWeeds)
            AdvanceGrowth(); // watered yesterday -> grows today

        if (!IsFullyGrown())
        {
            plotStatus = PlotStatus.Planted; // watering lapses, needs re-watering
            TryGrowWeeds();
        }

        interactable.RefreshPrompt();
        RefreshVisuals();
    }

    private void RemoveWeeds()
    {
        hasWeeds = false;
        weedParticles?.Play();
        SFXManager.Instance.PlaySound("Weeds");
        RefreshVisuals();
    }

    private void TryGrowWeeds()
    {
        if (hasWeeds || IsFullyGrown()) return;

        float roll = UnityEngine.Random.value;

        if (roll < weedChancePerDay)
        {
            hasWeeds = true;
            RefreshVisuals();
            interactable.RefreshPrompt();
        }
    }
}
