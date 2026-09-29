using System.Reflection;
using GardenGrowers.Tutorial;
using UnityEditor;
using UnityEngine;

namespace GardenGrowers.EditorTools
{
    /// <summary>Play mode window for testing the tutorial. Jump to any step, get the tools, or grow a tomato.</summary>
    public class TutorialDebugWindow : EditorWindow
    {
        private const string TomatoSeedsPath = "Assets/Data/Tools/Seeds/Tomato Seeds.asset";
        private static readonly string[] ToolPaths = { "Assets/Data/Tools/Shovel.asset", "Assets/Data/Tools/WateringCan.asset" };
        private const string CompostPath = "Assets/Data/Tools/Fert.asset";
        private const string TomatoPath = "Assets/Data/Items/Tomato.asset";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private Vector2 scroll;

        [MenuItem("Tools/Garden Growers/Tutorial/Debug Window (Play mode)", priority = 100)]
        public static void Open() => GetWindow<TutorialDebugWindow>("Tutorial Debug");

        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Press Play, then use these buttons to test parts of the tutorial.", MessageType.Info);
                return;
            }

            TutorialManager manager = TutorialManager.Instance;
            if (manager == null || manager.Sequence == null)
            {
                EditorGUILayout.HelpBox("There is no running tutorial in this scene.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Current step", manager.CurrentStep != null ? manager.CurrentStep.name : "-");
            EditorGUILayout.Space();

            if (GUILayout.Button("Give the spade, watering can, seeds and compost")) GiveEverything();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Young tomato (watered)")) GrowTomato(1);
                if (GUILayout.Button("Ripe tomato")) GrowTomato(-1);
            }
            if (GUILayout.Button("5 tomatoes in the bag (to test selling)")) GiveTomatoes();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Jump to step", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < manager.Sequence.steps.Count; i++)
            {
                TutorialStep step = manager.Sequence.steps[i];
                if (step == null) continue;
                using (new EditorGUI.DisabledScope(i == manager.CurrentIndex))
                {
                    if (GUILayout.Button(step.name)) manager.JumpTo(i);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Adds the tools the tutorial normally hands out, plus one seed and two compost.</summary>
        private static void GiveEverything()
        {
            if (InventoryManager.Instance == null) return;

            foreach (string path in ToolPaths)
            {
                var tool = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (tool != null && InventoryQuery.Count(tool) == 0) InventoryManager.Instance.AddItem(tool, 1, InventorySlot.InventoryType.Tool);
            }

            var seeds = AssetDatabase.LoadAssetAtPath<ItemData>(TomatoSeedsPath);
            var compost = AssetDatabase.LoadAssetAtPath<ItemData>(CompostPath);
            if (seeds != null) InventoryManager.Instance.AddItem(seeds, 1, InventorySlot.InventoryType.Tool);
            if (compost != null) InventoryManager.Instance.AddItem(compost, 2, InventorySlot.InventoryType.Tool);
            Debug.Log("[Tutorial Debug] Added the spade, watering can, 1 tomato seed and 2 compost.");
        }

        private static void GiveTomatoes()
        {
            var tomato = AssetDatabase.LoadAssetAtPath<ItemData>(TomatoPath);
            if (InventoryManager.Instance == null || tomato == null)
            {
                Debug.LogWarning("[Tutorial Debug] Couldn't find " + TomatoPath + ".");
                return;
            }

            InventoryManager.Instance.AddItem(tomato, 5, InventorySlot.InventoryType.Item);
            Debug.Log("[Tutorial Debug] Added 5 tomatoes to the bag.");
        }

        /// <summary>Puts a watered tomato in the bed nearest the player. Stage 0 = just planted, -1 = ripe.</summary>
        private static void GrowTomato(int stage)
        {
            var seed = AssetDatabase.LoadAssetAtPath<SeedData>(TomatoSeedsPath);
            var player = FindFirstObjectByType<FirstPersonController>();
            if (seed == null || seed.growthStagePrefabs == null || seed.growthStagePrefabs.Length == 0 || player == null)
            {
                Debug.LogWarning("[Tutorial Debug] Couldn't find the tomato seeds or the player.");
                return;
            }

            PlotScript nearest = null;
            float best = float.MaxValue;
            foreach (PlotScript plot in FindObjectsByType<PlotScript>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(plot.transform.position, player.transform.position);
                if (distance >= best) continue;
                best = distance;
                nearest = plot;
            }

            if (nearest == null) return;

            int last = seed.growthStagePrefabs.Length - 1;
            int index = stage < 0 ? last : Mathf.Clamp(stage, 0, last);

            // PlotScript keeps these private, so set them the same way planting would.
            SetField(nearest, "plantedSeed", seed);
            SetField(nearest, "growthStageIndex", index);
            if (TimeManager.Instance != null) SetField(nearest, "lastProcessedDay", GameTimeStamp.ToAbsoluteDay(TimeManager.Instance.GetGameTimeStamp()));
            nearest.plotStatus = PlotScript.PlotStatus.Watered;
            typeof(PlotScript).GetMethod("RefreshVisuals", Private)?.Invoke(nearest, null);
            (typeof(PlotScript).GetField("interactable", Private)?.GetValue(nearest) as Interactable)?.RefreshPrompt();

            Debug.Log("[Tutorial Debug] Tomato at stage " + index + " of " + last + " in " + nearest.name + ".", nearest);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, Private);
            if (field != null) field.SetValue(target, value);
            else Debug.LogWarning("[Tutorial Debug] PlotScript has no field called " + name + ".");
        }
    }
}
