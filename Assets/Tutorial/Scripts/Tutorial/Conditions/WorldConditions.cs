using System;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.Tutorial.Conditions
{
    /// <summary>Done when the player walks close to a target.</summary>
    [Serializable, MenuPath("World/Reach Target")]
    public class ReachTargetCondition : TutorialCondition
    {
        [Tooltip("A TutorialTarget id, or @gogo / @activePlot.")]
        public string targetId = "";
        [Tooltip("0 = use the target's own reach radius.")]
        [Min(0f)] public float radius;

        public override bool IsMet(TutorialContext context)
        {
            Transform target = context.ResolveTarget(targetId);
            if (target == null) return false;
            float r = radius > 0f ? radius : context.ReachRadiusFor(targetId);
            return context.FlatDistanceToPlayer(target) <= r;
        }
    }

    /// <summary>Done once a new day starts (after resting in the house).</summary>
    [Serializable, MenuPath("World/Day Passed")]
    public class DayPassedCondition : TutorialCondition
    {
        [Min(1)] public int days = 1;
        [Tooltip("Only count it once it's morning. Resting wakes you at 6:00, but just staying up past midnight doesn't count.")]
        public bool mustBeMorning = true;
        [Range(0, 23)] public int morningFromHour = 5;

        private int startDay;

        public override void Begin(TutorialContext context) => startDay = Today();

        public override bool IsMet(TutorialContext context)
        {
            if (Today() - startDay < days) return false;
            return !mustBeMorning || TimeManager.Instance == null || TimeManager.Instance.GetGameTimeStamp().hour >= morningFromHour;
        }

        private static int Today()
        {
            return TimeManager.Instance != null ? GameTimeStamp.ToAbsoluteDay(TimeManager.Instance.GetGameTimeStamp()) : 0;
        }
    }

    /// <summary>Done after a number of seconds. Handy for short cut-scene steps.</summary>
    [Serializable, MenuPath("World/Wait Seconds")]
    public class TimerCondition : TutorialCondition
    {
        [Min(0f)] public float seconds = 2f;

        private float startTime;

        public override void Begin(TutorialContext context) => startTime = Time.time;

        public override bool IsMet(TutorialContext context) => Time.time - startTime >= seconds;
    }

    /// <summary>Done when the player chases the bird off a plant before it eats. Also done if there's no bird.</summary>
    [Serializable, MenuPath("World/Bird Scared Away")]
    public class BirdScaredCondition : TutorialCondition
    {
        [Tooltip("The player must be this close to the bird when it flies off, or it doesn't count as chased away.")]
        [Min(1f)] public float chaseDistance = 6f;

        private bool attacked;
        private int eatenAtStart;
        private BirdEnum lastState;
        private readonly System.Collections.Generic.List<PlotWatcher> plantsAtStart = new System.Collections.Generic.List<PlotWatcher>();

        public override void Begin(TutorialContext context)
        {
            attacked = false;
            lastState = context.Bird != null ? context.Bird.currentState : BirdEnum.Idle;
            eatenAtStart = Eaten(context);
            plantsAtStart.Clear();
            foreach (PlotWatcher plot in context.Plots)
            {
                if (plot.HasPlant) plantsAtStart.Add(plot);
            }
        }

        public override bool IsMet(TutorialContext context)
        {
            BirdMovment bird = context.Bird;
            if (bird == null || !bird.gameObject.activeInHierarchy) return bird == null;

            BirdEnum state = bird.currentState;
            bool justChanged = state != lastState;
            if (justChanged && Application.isEditor) LogState(context, bird, lastState);
            lastState = state;

            if (state == BirdEnum.Target || state == BirdEnum.Eat) attacked = true;
            if (!attacked || state != BirdEnum.Flee) return false;

            // The bird also flees right after eating, so only count it if every plant is still there.
            foreach (PlotWatcher plot in plantsAtStart)
            {
                if (!plot.HasPlant) return false;
            }
            if (Eaten(context) != eatenAtStart) return false;

            // The bird can also fly off by itself. If the player is far away, reset and wait.
            float toPlayer = context.Player != null ? Vector3.Distance(bird.transform.position, context.Player.position) : 0f;
            if (toPlayer > chaseDistance)
            {
                attacked = false;
                return false;
            }
            return true;
        }

        private static int Eaten(TutorialContext context)
        {
            int total = 0;
            foreach (PlotWatcher plot in context.Plots) total += plot.TimesEaten;
            return total;
        }

        /// <summary>Editor only: prints what the bird is doing, to help tune the bird step.</summary>
        private static void LogState(TutorialContext context, BirdMovment bird, BirdEnum from)
        {
            const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var target = typeof(BirdMovment).GetField("targetTransform", Private)?.GetValue(bird) as Transform;
            var plot = typeof(BirdMovment).GetField("targetPlot", Private)?.GetValue(bird) as PlotScript;
            float toPlayer = context.Player != null ? Vector3.Distance(bird.transform.position, context.Player.position) : -1f;

            Debug.Log("[Tutorial] Bird " + from + " -> " + bird.currentState
                      + " at " + bird.transform.position.ToString("F1") + ", player " + toPlayer.ToString("F1") + " m away"
                      + (target != null ? ", target " + target.name : "")
                      + (plot != null ? " on " + plot.name + " (" + plot.plotStatus + ")" : ""), bird);
        }
    }

    /// <summary>True if the scene has no working bird. Use as a skip rule for the bird step.</summary>
    [Serializable, MenuPath("World/No Bird In Scene")]
    public class NoBirdCondition : TutorialCondition
    {
        public override bool IsMet(TutorialContext context) => context.Bird == null;
    }

    /// <summary>Done when Gogo has reached where she was walking to.</summary>
    [Serializable, MenuPath("Gogo/Gogo Arrived")]
    public class GogoArrivedCondition : TutorialCondition
    {
        public override bool IsMet(TutorialContext context)
        {
            return context.Gogo == null || context.Gogo.Mover == null || context.Gogo.Mover.HasArrived;
        }
    }
}
