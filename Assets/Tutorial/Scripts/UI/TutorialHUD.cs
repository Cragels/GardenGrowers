using System.Collections;
using System.Collections.Generic;
using GardenGrowers.CameraSystems;
using GardenGrowers.Core;
using TMPro;
using UnityEngine;

namespace GardenGrowers.UI
{
    /// <summary>The tutorial's screen layer: goal, key prompts, marker, pop-ups, fades and story cards.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)] // after the camera has moved this frame
    public class TutorialHUD : MonoBehaviour
    {
        public static TutorialHUD Instance { get; private set; }

        [SerializeField] private HudStyle style = new HudStyle();
        [SerializeField] private string panelTitle = "Gogo's Garden";
        [Tooltip("Draw order. Above the group's HUD (0).")]
        [SerializeField] private int sortingOrder = 20;

        private Canvas canvas;
        private ObjectivePanelView objective;
        private HintBarView hints;
        private ToastView toast;
        private ScreenFader fader;
        private StoryCardView story;
        private ObjectiveMarker marker;
        private BagSlotHighlighter bag;
        private SubtitleView subtitle;
        private TextMeshProUGUI dialogueHint;
        private TextMeshProUGUI banner;
        private float bannerTimer;

        public HudStyle Style => style;
        public bool IsBagOpen => BagSlotHighlighter.IsBagOpen;
        public Transform MarkerTarget => marker != null ? marker.Target : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Used by the setup tool to pass in the font and art.</summary>
        public void Configure(HudStyle newStyle)
        {
            style = newStyle;
        }

        private void Build()
        {
            if (style.font == null) style.font = TMP_Settings.defaultFontAsset;

            canvas = UIFactory.CreateScreenCanvas("Tutorial HUD Canvas", transform, sortingOrder, new Vector2(1920f, 1080f));
            Transform root = canvas.transform;

            marker = new ObjectiveMarker(root, style);
            objective = new ObjectivePanelView(root, style, panelTitle);
            hints = new HintBarView(root, style);
            toast = new ToastView(root, style);
            subtitle = new SubtitleView(root, style);
            bag = new BagSlotHighlighter(root, style, canvas);

            dialogueHint = UIFactory.CreateText("DialogueHint", root, style.font, 28f, style.text, TextAlignmentOptions.Right);
            dialogueHint.text = "E  Next      Hold E  Faster";
            UIFactory.Place(dialogueHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(600f, 40f));
            dialogueHint.alpha = 0f;

            banner = UIFactory.CreateText("Banner", root, style.font, 60f, style.text, TextAlignmentOptions.Center);
            UIFactory.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1600f, 90f));
            banner.alpha = 0f;

            story = new StoryCardView(root, style);
            fader = new ScreenFader(root, style);
        }

        private void LateUpdate()
        {
            // Stay out of the way of the pause menu.
            canvas.enabled = Time.timeScale > 0f;

            float dt = Time.unscaledDeltaTime;

            // The group's bag covers most of the screen, so hide the goal and marker and move the prompts down.
            bool bagOpen = BagSlotHighlighter.IsBagOpen;
            objective.SetSuppressed(bagOpen);
            marker.SetSuppressed(bagOpen);
            hints.SetDocked(bagOpen);
            subtitle.SetSuppressed(bagOpen);

            objective.Tick(dt);
            hints.Tick(dt);
            toast.Tick(dt);
            marker.Tick(dt);
            subtitle.Tick(dt);
            bag.Tick();

            bool talking = CameraFocus.Instance != null && CameraFocus.Instance.IsFocusing;
            dialogueHint.alpha = Mathf.MoveTowards(dialogueHint.alpha, talking ? 1f : 0f, dt * 4f);

            if (bannerTimer > 0f) bannerTimer -= dt;
            banner.alpha = Mathf.MoveTowards(banner.alpha, bannerTimer > 0f ? 1f : 0f, dt * 2f);
        }

        //Goal

        public void SetObjective(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) objective.Hide();
            else objective.Show(text);
        }

        public void SetProgress(string text) => objective.SetProgress(text);
        public void CompleteObjective() => objective.Complete();
        public void HideObjective() => objective.Hide();

        //Control prompts

        public void SetHints(IList<ControlHint> list) => hints.Set(list);
        public void ShowHints() => hints.Show();
        public void HideHints() => hints.Hide();
        public void PulseHint(string key) => hints.Pulse(key);
        public bool HintsVisible => hints.IsVisible;

        //Marker and highlight

        public void Track(Transform target, float heightOffset = 1.2f) => marker.Track(target, heightOffset);
        public void ClearMarker() => marker.Clear();
        public void HighlightBagItem(ItemData item) => bag.Highlight(item);
        public void ClearBagHighlight() => bag.Clear();

        //Subtitles

        public void WatchSpeaker(GardenGrowers.Dialogue.DialogueSpeaker speaker) => subtitle.Watch(speaker);

        //Pop-ups and story

        public void ShowToast(string heading, string text, Sprite icon) => toast.Show(heading, text, icon);

        public void ShowBanner(string text, float seconds = 3.5f)
        {
            banner.text = text;
            bannerTimer = seconds;
        }

        public IEnumerator Fade(float alpha, float seconds, string caption = null, string subtitle = null)
        {
            if (caption != null || subtitle != null) fader.SetText(caption, subtitle);
            yield return fader.FadeTo(alpha, seconds);
            if (alpha <= 0f) fader.SetText(null, null);
        }

        /// <summary>Change the words on the black screen without fading.</summary>
        public void SetFadeText(string caption, string subtitle) => fader.SetText(caption, subtitle);

        public IEnumerator ShowStory(string heading, string text, Sprite icon, bool waitForKey, float autoSeconds = 4f)
        {
            yield return story.Show(heading, text, icon, waitForKey, autoSeconds);
        }
    }
}
