using System;
using System.Collections;
using System.Collections.Generic;
using GardenGrowers.CameraSystems;
using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using GardenGrowers.FX;
using GardenGrowers.NPC;
using GardenGrowers.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GardenGrowers.Tutorial
{
    /// <summary>Runs the tutorial one step at a time, waiting for the player to do each one.</summary>
    [DisallowMultipleComponent]
    public class TutorialManager : MonoBehaviour
    {
        public const string CompletedPrefsKey = "GardenGrowers.TutorialCompleted";

        public static TutorialManager Instance { get; private set; }

        [SerializeField] private TutorialSequence sequence = null;
        [SerializeField] private GrandmaController gogo = null;
        [SerializeField] private TutorialHUD hud = null;
        [SerializeField] private TutorialFX fx = null;
        [Tooltip("All seed types, so plant growth can be read. The setup tool fills this.")]
        [SerializeField] private SeedData[] seedCatalog = new SeedData[0];
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0f)] private float startDelay = 1f;

        [Header("Testing")]
        [Tooltip("Start from a later step (0 = the beginning). Earlier steps' items are not given.")]
        [SerializeField, Min(0)] private int startAtStep = 0;
        [Tooltip("Press F8 in Play mode to skip the current step.")]
        [SerializeField] private bool allowSkipKey = true;

        private enum StepOutcome { Completed, Skipped, Failed }

        private sealed class StepResult
        {
            public StepOutcome outcome = StepOutcome.Completed;
        }

        private bool skipRequested;
        private int jumpToIndex = -1;

        public TutorialContext Context { get; private set; }
        public TutorialSequence Sequence => sequence;
        public TutorialStep CurrentStep { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        public bool IsRunning { get; private set; }

        public event Action<TutorialStep> StepStarted;
        public event Action<TutorialStep> StepCompleted;
        public event Action Finished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (gogo == null) gogo = FindFirstObjectByType<GrandmaController>();
            if (hud == null) hud = FindFirstObjectByType<TutorialHUD>();
            if (fx == null) fx = FindFirstObjectByType<TutorialFX>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            BuildContext();
            if (!playOnStart) yield break;
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
            yield return Run();
        }

        /// <summary>Start the tutorial by hand (if Play On Start is off).</summary>
        public void Begin()
        {
            if (IsRunning) return;
            if (Context == null) BuildContext();
            StartCoroutine(Run());
        }

        /// <summary>Skip the current step (debug key F8, or call from a button).</summary>
        public void SkipCurrentStep() => skipRequested = true;

        /// <summary>For testing (the Tutorial Debug window). The current step's rewards are not given.</summary>
        public void JumpTo(int stepIndex)
        {
            if (!IsRunning || sequence == null || stepIndex < 0 || stepIndex >= sequence.steps.Count) return;
            jumpToIndex = stepIndex;
            skipRequested = true;
            if (gogo != null) gogo.StopTalking(); // cut a long conversation short
        }

        private void Update()
        {
            if (allowSkipKey && IsRunning && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) SkipCurrentStep();
        }

        private void BuildContext()
        {
            PlotWatcher.SetSeedCatalog(seedCatalog);

            var plots = new List<PlotWatcher>();
            foreach (PlotScript plot in FindObjectsByType<PlotScript>(FindObjectsSortMode.None))
            {
                PlotWatcher watcher = plot.GetComponent<PlotWatcher>();
                if (watcher == null) watcher = plot.gameObject.AddComponent<PlotWatcher>();
                plots.Add(watcher);
            }

            Context = new TutorialContext(this, gogo, hud, fx, plots);
            if (gogo != null && gogo.Speaker != null) gogo.Speaker.TextFormatter = Context.Format;
            if (hud != null && gogo != null && gogo.Speaker != null) hud.WatchSpeaker(gogo.Speaker);
        }

        //The sequence

        private IEnumerator Run()
        {
            if (sequence == null || sequence.steps.Count == 0)
            {
                Debug.LogWarning("[Tutorial] No tutorial sequence assigned.", this);
                yield break;
            }

            IsRunning = true;
            int index = Mathf.Clamp(startAtStep, 0, sequence.steps.Count - 1);

            while (index >= 0 && index < sequence.steps.Count)
            {
                TutorialStep step = sequence.steps[index];
                if (step == null)
                {
                    index++;
                    continue;
                }

                CurrentIndex = index;
                CurrentStep = step;
                Context.CurrentStep = step;

                var result = new StepResult();
                yield return RunStep(step, result);

                if (jumpToIndex >= 0)
                {
                    index = jumpToIndex;
                    jumpToIndex = -1;
                    continue;
                }

                if (step.endsTutorial && result.outcome != StepOutcome.Failed) break;
                index = NextIndex(step, index, result.outcome);
            }

            Finish();
        }

        private int NextIndex(TutorialStep step, int index, StepOutcome outcome)
        {
            if (outcome == StepOutcome.Failed && step.onFailGoTo != null) return sequence.IndexOf(step.onFailGoTo);
            if (step.nextOverride != null) return sequence.IndexOf(step.nextOverride);
            return index + 1;
        }

        private IEnumerator RunStep(TutorialStep step, StepResult result)
        {
            skipRequested = false;

            // 1. Skip?
            if (step.skipIf != null)
            {
                step.skipIf.Begin(Context);
                if (step.skipIf.IsMet(Context))
                {
                    result.outcome = StepOutcome.Skipped;
                    yield break;
                }
            }

            StepStarted?.Invoke(step);

            // Steps with no goal (like the ending) shouldn't keep showing the last one.
            if (hud != null && string.IsNullOrWhiteSpace(step.objective)) hud.HideObjective();

            // 2. Start actions, then Gogo introduces the goal.
            yield return RunActions(step.onStart);
            UpdatePointTarget(step);

            if (gogo != null && step.gogo == GogoDirective.ApproachPlayer) yield return ApproachWithCamera(step);

            if (gogo != null && step.intro != null && !step.intro.IsEmpty)
            {
                if (step.intro.focusCamera)
                {
                    // Full conversation: she stops, the camera turns to her, and we wait.
                    CloseBagIfOpen();
                    gogo.StandStill();
                    yield return gogo.Say(step.intro);
                }
                else
                {
                    // Quick comment: she talks while the player keeps playing.
                    gogo.Comment(step.intro);
                }
            }

            yield return RunActions(step.afterIntro);
            ApplyGogoDirective(step);

            // 3. Show the goal.
            ShowStepHud(step);

            // 4. Wait for the player.
            step.completeWhen?.Begin(Context);
            step.failIf?.Begin(Context);
            float stuckTimer = 0f;
            int nudges = 0;

            while (true)
            {
                if (skipRequested)
                {
                    if (gogo != null) gogo.StopTalking();
                    result.outcome = jumpToIndex >= 0 ? StepOutcome.Skipped : StepOutcome.Completed;
                    break;
                }

                if (step.failIf != null && step.failIf.IsMet(Context))
                {
                    result.outcome = StepOutcome.Failed;
                    break;
                }

                if (step.completeWhen == null || step.completeWhen.IsMet(Context))
                {
                    result.outcome = StepOutcome.Completed;
                    break;
                }

                KeepMarkerOnTarget(step);
                if (hud != null)
                {
                    hud.SetProgress(step.completeWhen.Progress(Context));
                    if (step.highlightInBag != null && hud.HintsVisible) hud.PulseHint(hud.IsBagOpen ? null : "Tab");
                }

                bool paused = (gogo != null && gogo.IsTalking) || (PlayerControlLock.Instance != null && PlayerControlLock.Instance.IsLocked);
                if (!paused) stuckTimer += Time.deltaTime;
                if (step.hintTiming != HintTiming.Never && stuckTimer > step.stuckSeconds * (nudges + 1))
                {
                    nudges++;
                    OnStuck(step);
                }

                yield return null;
            }

            // 5. Wrap up.
            if (hud != null)
            {
                hud.ClearMarker();
                hud.HideHints();
                hud.ClearBagHighlight();
                hud.SetProgress(null);
            }

            if (result.outcome == StepOutcome.Failed || result.outcome == StepOutcome.Skipped)
            {
                if (hud != null) hud.HideObjective();
                yield break;
            }

            if (hud != null && !string.IsNullOrWhiteSpace(step.objective)) hud.CompleteObjective();
            yield return RunActions(step.onComplete);

            if (step.celebrate) Celebrate();
            else if (fx != null && !string.IsNullOrWhiteSpace(step.objective)) fx.Play(Sfx.Chime, 0.5f);

            StepCompleted?.Invoke(step);

            if (gogo != null && step.success != null && !step.success.IsEmpty) yield return gogo.Say(step.success);
        }

        /// <summary>Gogo walks up to the player. If a conversation follows, the camera watches her come.</summary>
        private IEnumerator ApproachWithCamera(TutorialStep step)
        {
            object token = new object();
            bool watch = step.intro != null && step.intro.focusCamera && CameraFocus.Instance != null && gogo.Speaker != null;
            if (watch) CameraFocus.Instance.BeginFocus(token, gogo.Speaker.LookPoint);

            yield return gogo.ApproachPlayer(3.2f, 20f);

            // If Gogo speaks next, she takes over the camera focus and hands it back herself.
            if (watch && (step.intro == null || step.intro.IsEmpty)) CameraFocus.Instance.EndFocus(token);
        }

        private IEnumerator RunActions(List<TutorialAction> actions)
        {
            if (actions == null) yield break;
            foreach (TutorialAction action in actions)
            {
                if (action == null) continue;
                yield return action.Execute(Context);
            }
        }

        private void ShowStepHud(TutorialStep step)
        {
            if (hud == null) return;

            hud.SetObjective(Context.Format(step.objective));
            hud.SetHints(step.hints);
            if (step.hintTiming == HintTiming.Immediately)
            {
                hud.ShowHints();
                if (step.highlightInBag != null) hud.HighlightBagItem(step.highlightInBag);
            }
            else
            {
                hud.HideHints();
            }

            Transform target = Context.ResolveTarget(step.targetId);
            if (target != null) hud.Track(target, Context.MarkerHeightFor(step.targetId));
            else hud.ClearMarker();

        }

        private void KeepMarkerOnTarget(TutorialStep step)
        {
            // Targets like @activePlot or @bird can change while the step runs.
            if (hud == null || string.IsNullOrEmpty(step.targetId) || !step.targetId.StartsWith("@")) return;
            Transform target = Context.ResolveTarget(step.targetId);
            if (target != hud.MarkerTarget)
            {
                if (target != null) hud.Track(target, Context.MarkerHeightFor(step.targetId));
                else hud.ClearMarker();
                if (gogo != null) gogo.PointTarget = target;
            }
        }

        /// <summary>A conversation with the bag open would leave the cursor free and the view stuck.</summary>
        private static void CloseBagIfOpen()
        {
            if (BagSlotHighlighter.IsBagOpen && UIManager.Instance != null) UIManager.Instance.InventoryUIControl();
        }

        private void UpdatePointTarget(TutorialStep step)
        {
            if (gogo != null) gogo.PointTarget = Context.ResolveTarget(step.targetId);
        }

        private void ApplyGogoDirective(TutorialStep step)
        {
            if (gogo == null) return;

            switch (step.gogo)
            {
                case GogoDirective.FollowPlayer:
                case GogoDirective.ApproachPlayer:
                    gogo.FollowPlayer();
                    break;
                case GogoDirective.GoToTarget:
                    Transform spot = Context.ResolveTarget(step.gogoTargetId);
                    if (spot != null) gogo.GoTo(spot.position, 0.4f);
                    break;
                case GogoDirective.StayPut:
                    gogo.StandStill();
                    break;
            }
        }

        private void OnStuck(TutorialStep step)
        {
            if (hud != null)
            {
                hud.ShowHints();
                if (step.highlightInBag != null) hud.HighlightBagItem(step.highlightInBag);
            }

            if (gogo == null || gogo.IsTalking) return;

            if (step.nudge != null && !step.nudge.IsEmpty) gogo.Comment(step.nudge);
        }

        private void Celebrate()
        {
            if (gogo != null) gogo.Celebrate();
            if (fx != null) fx.Celebrate();
        }

        private void Finish()
        {
            IsRunning = false;
            CurrentStep = null;
            if (hud != null) hud.HideObjective();
            if (TimeManager.Instance != null && Context != null) TimeManager.Instance.timeScale = Context.OriginalClockSpeed;
            PlayerPrefs.SetInt(CompletedPrefsKey, 1);
            PlayerPrefs.Save();
            Finished?.Invoke();
        }
    }
}
