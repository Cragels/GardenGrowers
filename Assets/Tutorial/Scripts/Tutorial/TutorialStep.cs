using System.Collections.Generic;
using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using GardenGrowers.UI;
using UnityEngine;

namespace GardenGrowers.Tutorial
{
    /// <summary>What Gogo does while a step is active.</summary>
    public enum GogoDirective
    {
        FollowPlayer,
        /// <summary>Walk up to the player before talking, then follow.</summary>
        ApproachPlayer,
        /// <summary>Walk to a target and wait there (like her stall).</summary>
        GoToTarget,
        StayPut
    }

    /// <summary>When the control prompts appear.</summary>
    public enum HintTiming
    {
        /// <summary>Use for brand new mechanics.</summary>
        Immediately,
        /// <summary>Use for "test" steps, Portal style.</summary>
        WhenStuck,
        Never
    }

    /// <summary>One tutorial step, saved as an asset: the goal, what Gogo says, the prompts and the rules.</summary>
    [CreateAssetMenu(menuName = "Garden Growers/Tutorial/Tutorial Step", fileName = "New Step")]
    public class TutorialStep : ScriptableObject
    {
        [Header("Goal shown on the HUD (leave empty for story-only steps)")]
        public string objective = "";
        [Tooltip("For the team: why this step exists and what it teaches.")]
        [TextArea(2, 4)] public string designNote = "";

        [Header("Where")]
        [Tooltip("Gets the marker and is what Gogo points at. A TutorialTarget id, or @gogo, @activePlot, @bird.")]
        public string targetId = "";
        public GogoDirective gogo = GogoDirective.FollowPlayer;
        [Tooltip("For Go To Target: where Gogo walks to and waits.")]
        public string gogoTargetId = "";

        [Header("What Gogo says")]
        public DialogueSequence intro = new DialogueSequence();
        [Tooltip("Said when the step is done.")]
        public DialogueSequence success = new DialogueSequence();
        [Tooltip("Said if the player seems stuck.")]
        public DialogueSequence nudge = new DialogueSequence();

        [Header("Control prompts (HUD, not dialogue)")]
        public List<ControlHint> hints = new List<ControlHint>();
        public HintTiming hintTiming = HintTiming.Immediately;
        [Tooltip("Seconds without progress before Gogo nudges and prompts appear.")]
        [Min(1f)] public float stuckSeconds = 15f;
        [Tooltip("Pulse this item's bag slot so the player knows what to click.")]
        public ItemData highlightInBag;

        [Header("Rules")]
        [Tooltip("The step is done when this is true. Empty = done right after the intro.")]
        [SerializeReference, SubclassSelector] public TutorialCondition completeWhen;
        [Tooltip("Checked before the step starts. If true, the step is skipped.")]
        [SerializeReference, SubclassSelector] public TutorialCondition skipIf;
        [Tooltip("If this becomes true, the step fails and jumps to On Fail Go To.")]
        [SerializeReference, SubclassSelector] public TutorialCondition failIf;
        public TutorialStep onFailGoTo;

        [Header("Actions")]
        [Tooltip("Before Gogo speaks.")]
        [SerializeReference, SubclassSelector] public List<TutorialAction> onStart = new List<TutorialAction>();
        [Tooltip("After Gogo speaks, before the player acts.")]
        [SerializeReference, SubclassSelector] public List<TutorialAction> afterIntro = new List<TutorialAction>();
        [Tooltip("When the step is done, before Gogo's praise.")]
        [SerializeReference, SubclassSelector] public List<TutorialAction> onComplete = new List<TutorialAction>();

        [Header("Flow")]
        [Tooltip("Gogo hops and claps, sparkles and a chime.")]
        public bool celebrate;
        [Tooltip("Jump here next instead of the next step in the list.")]
        public TutorialStep nextOverride;
        [Tooltip("The tutorial ends after this step.")]
        public bool endsTutorial;
    }
}
