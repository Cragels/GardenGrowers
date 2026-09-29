using System;
using System.Collections.Generic;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.Tutorial.Conditions
{
    /// <summary>Done when enough plots are at this status or later. Any plot counts, even ones done early.</summary>
    [Serializable, MenuPath("Garden/Plot Reaches Status")]
    public class PlotStatusCondition : TutorialCondition
    {
        [Tooltip("Counts as done at this status or later (Dug < Planted < Watered).")]
        public PlotScript.PlotStatus atLeast = PlotScript.PlotStatus.Dug;
        [Min(1)] public int plots = 1;
        [Tooltip("Only count plots that reach the status after the step starts.")]
        public bool onlyNewChanges;

        private readonly Dictionary<PlotWatcher, PlotScript.PlotStatus> startStatus = new Dictionary<PlotWatcher, PlotScript.PlotStatus>();

        public override void Begin(TutorialContext context)
        {
            startStatus.Clear();
            foreach (PlotWatcher plot in context.Plots) startStatus[plot] = plot.Status;
        }

        public override bool IsMet(TutorialContext context) => CountReached(context) >= plots;

        public override string Progress(TutorialContext context)
        {
            return plots > 1 ? Mathf.Min(CountReached(context), plots) + " / " + plots : null;
        }

        private int CountReached(TutorialContext context)
        {
            int count = 0;
            foreach (PlotWatcher plot in context.Plots)
            {
                if (!startStatus.TryGetValue(plot, out PlotScript.PlotStatus before)) before = PlotScript.PlotStatus.Empty;
                bool newlyReached = !onlyNewChanges || (int)before < (int)atLeast;
                if (newlyReached && (int)plot.Status >= (int)atLeast)
                {
                    count++;
                    context.ActivePlot = plot;
                }
            }
            return count;
        }
    }

    /// <summary>Done when no growing plant has weeds any more.</summary>
    [Serializable, MenuPath("Garden/Weeds Cleared")]
    public class WeedsClearedCondition : TutorialCondition
    {
        public override bool IsMet(TutorialContext context)
        {
            foreach (PlotWatcher plot in context.Plots)
            {
                if (plot.HasWeeds) return false;
            }
            return true;
        }
    }

    /// <summary>True while any plot has weeds. Use it inside "Not" to skip the weeds step.</summary>
    [Serializable, MenuPath("Garden/Weeds Present")]
    public class WeedsPresentCondition : TutorialCondition
    {
        public override bool IsMet(TutorialContext context)
        {
            foreach (PlotWatcher plot in context.Plots)
            {
                if (!plot.HasWeeds) continue;
                context.ActivePlot = plot;
                return true;
            }
            return false;
        }
    }

    /// <summary>Done when any plant is fully grown and ready to harvest.</summary>
    [Serializable, MenuPath("Garden/Plant Fully Grown")]
    public class PlantFullyGrownCondition : TutorialCondition
    {
        public override bool IsMet(TutorialContext context)
        {
            foreach (PlotWatcher plot in context.Plots)
            {
                if (!plot.HasPlant || !plot.IsFullyGrown) continue;
                context.ActivePlot = plot;
                return true;
            }
            return false;
        }
    }

    /// <summary>Done when the player has harvested this many times (since the step started, if ticked).</summary>
    [Serializable, MenuPath("Garden/Harvested")]
    public class HarvestedCondition : TutorialCondition
    {
        [Min(1)] public int times = 1;
        public bool sinceStepStart;

        private int start;

        public override void Begin(TutorialContext context) => start = sinceStepStart ? Total(context) : 0;

        public override bool IsMet(TutorialContext context) => Total(context) - start >= times;

        private static int Total(TutorialContext context)
        {
            int total = 0;
            foreach (PlotWatcher plot in context.Plots) total += plot.TimesHarvested;
            return total;
        }
    }

    /// <summary>True when a bird eats a plant during the step. Used as a fail rule.</summary>
    [Serializable, MenuPath("Garden/Plant Eaten")]
    public class PlantEatenCondition : TutorialCondition
    {
        private int start;

        public override void Begin(TutorialContext context) => start = Total(context);

        public override bool IsMet(TutorialContext context) => Total(context) > start;

        private static int Total(TutorialContext context)
        {
            int total = 0;
            foreach (PlotWatcher plot in context.Plots) total += plot.TimesEaten;
            return total;
        }
    }
}
