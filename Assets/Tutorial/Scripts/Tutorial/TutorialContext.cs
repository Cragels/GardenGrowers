using System.Collections;
using System.Collections.Generic;
using GardenGrowers.FX;
using GardenGrowers.NPC;
using GardenGrowers.UI;
using UnityEngine;

namespace GardenGrowers.Tutorial
{
    /// <summary>Everything a condition or action might need, like Gogo, the HUD, the player and the plots.</summary>
    public class TutorialContext
    {
        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();
        private readonly List<PlotWatcher> plots;

        public TutorialContext(TutorialManager manager, GrandmaController gogo, TutorialHUD hud, TutorialFX fx, List<PlotWatcher> plots)
        {
            Manager = manager;
            Gogo = gogo;
            Hud = hud;
            Fx = fx;
            this.plots = plots ?? new List<PlotWatcher>();

            var controller = Object.FindFirstObjectByType<FirstPersonController>();
            Player = controller != null ? controller.transform : null;
            Camera = Camera.main;
            Bird = Object.FindFirstObjectByType<BirdMovment>(FindObjectsInactive.Include);
            OriginalClockSpeed = TimeManager.Instance != null ? TimeManager.Instance.timeScale : 6f;

            foreach (PlotWatcher plot in this.plots) plot.StatusChanged += OnPlotChanged;
        }

        public TutorialManager Manager { get; }
        public GrandmaController Gogo { get; }
        public TutorialHUD Hud { get; }
        public TutorialFX Fx { get; }
        public Transform Player { get; }
        public Camera Camera { get; }
        public BirdMovment Bird { get; }
        public IReadOnlyList<PlotWatcher> Plots => plots;
        public TutorialStep CurrentStep { get; internal set; }
        public float OriginalClockSpeed { get; }

        /// <summary>The plot the player worked on most recently. Markers follow it.</summary>
        public PlotWatcher ActivePlot { get; set; }

        public BirdSettingsBackup BirdBackup { get; set; }

        private void OnPlotChanged(PlotWatcher plot, PlotScript.PlotStatus from, PlotScript.PlotStatus to)
        {
            if (to != PlotScript.PlotStatus.Empty) ActivePlot = plot;
        }

        public Coroutine Run(IEnumerator routine) => Manager.StartCoroutine(routine);

        //Text variables like {earned}

        public void SetVariable(string key, string value) => variables[key] = value;

        public string Format(string text)
        {
            if (string.IsNullOrEmpty(text) || variables.Count == 0) return text;
            foreach (KeyValuePair<string, string> pair in variables) text = text.Replace("{" + pair.Key + "}", pair.Value);
            return text;
        }

        //Finding things by id

        /// <summary>Takes @gogo, @player, @activePlot, @bird, or any TutorialTarget id in the scene.</summary>
        public Transform ResolveTarget(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            switch (id.Trim().ToLowerInvariant())
            {
                case "@gogo":
                    return Gogo != null ? Gogo.transform : null;
                case "@player":
                    return Player;
                case "@bird":
                    return Bird != null && Bird.gameObject.activeInHierarchy ? Bird.transform : null;
                case "@activeplot":
                    if (ActivePlot != null) return ActivePlot.transform;
                    TutorialTarget fallback = TutorialTarget.Find("plot");
                    return fallback != null ? fallback.transform : null;
            }

            TutorialTarget target = TutorialTarget.Find(id);
            return target != null ? target.transform : null;
        }

        public float MarkerHeightFor(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 1.2f;
            string key = id.Trim().ToLowerInvariant();
            if (key == "@gogo") return Gogo != null && Gogo.Animator != null ? Gogo.Animator.Height + 0.6f : 3f;
            if (key == "@bird") return 0.8f;
            if (key == "@activeplot") return 1.6f;
            TutorialTarget target = TutorialTarget.Find(id);
            return target != null ? target.MarkerHeight : 1.2f;
        }

        public float ReachRadiusFor(string id)
        {
            TutorialTarget target = TutorialTarget.Find(id);
            return target != null ? target.ReachRadius : 3.5f;
        }

        public float FlatDistanceToPlayer(Transform target)
        {
            if (target == null || Player == null) return float.MaxValue;
            Vector3 d = target.position - Player.position;
            d.y = 0f;
            return d.magnitude;
        }
    }

    /// <summary>The bird's original numbers, restored after the tutorial borrows it.</summary>
    public class BirdSettingsBackup
    {
        public float searchDelay;
        public float eatDuration;
        public int speed;
        public bool wasActive;
    }
}
