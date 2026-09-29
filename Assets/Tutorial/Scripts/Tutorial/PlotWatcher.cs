using System;
using UnityEngine;

namespace GardenGrowers.Tutorial
{
    /// <summary>
    /// Watches one plot and fires events (status changes, weeds, growth, harvest, eaten by a bird).
    /// It only reads the plot, so PlotScript needs no changes. The tutorial adds one to each plot.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlotWatcher : MonoBehaviour
    {
        private static SeedData[] seedCatalog = Array.Empty<SeedData>();

        [SerializeField] private PlotScript plot;
        [Tooltip("The plot's weed model (child called 'Weed').")]
        [SerializeField] private GameObject weedVisual;
        [Tooltip("The plot's plant anchor (child called 'Anchor').")]
        [SerializeField] private Transform plantAnchor;

        private PlotScript.PlotStatus lastStatus;
        private bool lastWeeds;
        private string lastPlantName;
        private int cropCountLastFrame;

        public PlotScript Plot => plot;
        public PlotScript.PlotStatus Status => plot != null ? plot.plotStatus : PlotScript.PlotStatus.Empty;
        public bool HasPlant => Status == PlotScript.PlotStatus.Planted || Status == PlotScript.PlotStatus.Watered;
        public bool HasWeeds => weedVisual != null && weedVisual.activeSelf && HasPlant;
        public SeedData PlantedSeed { get; private set; }
        /// <summary>0 = just planted, last index = ready. -1 = nothing growing.</summary>
        public int GrowthStage { get; private set; } = -1;
        public bool IsFullyGrown => PlantedSeed != null && PlantedSeed.growthStagePrefabs != null && GrowthStage >= PlantedSeed.growthStagePrefabs.Length - 1;
        public int TimesHarvested { get; private set; }
        public int TimesEaten { get; private set; }

        public event Action<PlotWatcher, PlotScript.PlotStatus, PlotScript.PlotStatus> StatusChanged;
        public event Action<PlotWatcher> WeedsAppeared;
        public event Action<PlotWatcher> WeedsCleared;
        public event Action<PlotWatcher> Grew;
        public event Action<PlotWatcher> Harvested;
        public event Action<PlotWatcher> Eaten;

        /// <summary>All seed types, so the watcher can tell which growth stage a plant model is.</summary>
        public static void SetSeedCatalog(SeedData[] seeds)
        {
            seedCatalog = seeds ?? Array.Empty<SeedData>();
        }

        private void Awake()
        {
            if (plot == null) plot = GetComponent<PlotScript>();
            if (weedVisual == null)
            {
                Transform weed = transform.Find("Weed");
                if (weed != null) weedVisual = weed.gameObject;
            }
            if (plantAnchor == null) plantAnchor = transform.Find("Anchor");

            // If a teammate renames those children, read PlotScript's own references instead.
            if (weedVisual == null) weedVisual = ReadPlotField<GameObject>("weedVisual");
            if (plantAnchor == null) plantAnchor = ReadPlotField<Transform>("plantModelAnchor");
            if (plantAnchor == null) Debug.LogWarning("[Tutorial] PlotWatcher couldn't find the plant anchor on " + name + ", so growth can't be tracked.", this);
        }

        private T ReadPlotField<T>(string fieldName) where T : class
        {
            if (plot == null) return null;
            var field = typeof(PlotScript).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            return field != null ? field.GetValue(plot) as T : null;
        }

        private void Start()
        {
            lastStatus = Status;
            lastWeeds = HasWeeds;
            lastPlantName = CurrentPlantName();
            Resolve(lastPlantName, out SeedData seed, out int stage);
            PlantedSeed = seed;
            GrowthStage = stage;
            cropCountLastFrame = CropCount();
        }

        private void LateUpdate()
        {
            if (plot == null) return;

            // The plot swaps the plant model when it grows.
            string plantName = CurrentPlantName();
            if (plantName != lastPlantName)
            {
                lastPlantName = plantName;
                if (Resolve(plantName, out SeedData seed, out int stage))
                {
                    bool grew = seed == PlantedSeed && stage > GrowthStage;
                    PlantedSeed = seed;
                    GrowthStage = stage;
                    if (grew) Grew?.Invoke(this);
                }
            }

            PlotScript.PlotStatus status = plot.plotStatus;
            if (status != lastStatus)
            {
                PlotScript.PlotStatus old = lastStatus;
                lastStatus = status;

                bool hadPlant = old == PlotScript.PlotStatus.Planted || old == PlotScript.PlotStatus.Watered;
                if (status == PlotScript.PlotStatus.Empty && hadPlant)
                {
                    // Harvest adds crops to the bag. A bird does not.
                    if (CropCount() > cropCountLastFrame)
                    {
                        TimesHarvested++;
                        Harvested?.Invoke(this);
                    }
                    else
                    {
                        TimesEaten++;
                        Eaten?.Invoke(this);
                    }

                    PlantedSeed = null;
                    GrowthStage = -1;
                }

                StatusChanged?.Invoke(this, old, status);
            }

            bool weeds = HasWeeds;
            if (weeds != lastWeeds)
            {
                lastWeeds = weeds;
                if (weeds) WeedsAppeared?.Invoke(this);
                else WeedsCleared?.Invoke(this);
            }

            cropCountLastFrame = CropCount();
        }

        private int CropCount()
        {
            if (PlantedSeed == null || PlantedSeed.cropToYield == null) return 0;
            return InventoryQuery.Count(PlantedSeed.cropToYield);
        }

        private string CurrentPlantName()
        {
            if (plantAnchor == null) return null;
            // PlotScript spawns the stage prefab under the anchor, so its name ends in "(Clone)".
            for (int i = plantAnchor.childCount - 1; i >= 0; i--)
            {
                Transform child = plantAnchor.GetChild(i);
                if (child.name.EndsWith("(Clone)", StringComparison.Ordinal)) return child.name;
            }
            return null;
        }

        private static bool Resolve(string modelName, out SeedData seed, out int stage)
        {
            seed = null;
            stage = -1;
            if (string.IsNullOrEmpty(modelName)) return false;

            string baseName = modelName.Replace("(Clone)", string.Empty).Trim();
            foreach (SeedData candidate in seedCatalog)
            {
                if (candidate == null || candidate.growthStagePrefabs == null) continue;
                for (int i = 0; i < candidate.growthStagePrefabs.Length; i++)
                {
                    GameObject prefab = candidate.growthStagePrefabs[i];
                    if (prefab == null || prefab.name != baseName) continue;
                    seed = candidate;
                    stage = i;
                    return true;
                }
            }
            return false;
        }
    }
}
