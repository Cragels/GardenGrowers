using GardenGrowers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenGrowers.Dialogue
{
    /// <summary>A speech bubble above a character that faces the camera and types out its text.</summary>
    [DisallowMultipleComponent]
    public class SpeechBubble : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("The group's bar art (UI_Assets/Slot.png).")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField, Min(8f)] private float fontSize = 34f;
        [SerializeField, Min(100f)] private float maxTextWidth = 560f;
        [SerializeField] private Vector2 padding = new Vector2(30f, 20f);

        [Header("Placement")]
        [Tooltip("What the bubble follows. Leave empty to follow the parent.")]
        [SerializeField] private Transform anchor = null;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
        [Tooltip("World size of one UI unit at the reference distance.")]
        [SerializeField, Min(0.0001f)] private float worldScale = 0.0035f;
        [Tooltip("The bubble grows a little when far away so it stays readable.")]
        [SerializeField, Min(0.1f)] private float referenceDistance = 4f;
        [SerializeField] private Vector2 distanceScaleRange = new Vector2(0.85f, 2.2f);

        private RectTransform scaler;
        private CanvasGroup group;
        private RectTransform panel;
        private TextMeshProUGUI body;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI continueText;

        private bool visible;
        private Camera mainCamera;
        private bool built;
        private Typewriter typewriter;

        public Typewriter Typewriter
        {
            get
            {
                Build();
                return typewriter;
            }
        }

        public bool IsVisible => visible;

        /// <summary>The bubble's panel, for checking if it's on screen.</summary>
        public RectTransform Panel
        {
            get
            {
                Build();
                return panel;
            }
        }

        private void Awake()
        {
            Build();
            scaler.gameObject.SetActive(false);
        }

        /// <summary>Used by the editor tools to pass in the font and art.</summary>
        public void Configure(TMP_FontAsset newFont, Sprite newPanelSprite)
        {
            font = newFont;
            panelSprite = newPanelSprite;
        }

        private void Build()
        {
            if (built) return;
            built = true;

            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            var rootRect = (RectTransform)transform;
            rootRect.sizeDelta = new Vector2(10f, 10f);

            scaler = UIFactory.CreateRect("Scaler", transform);
            UIFactory.Place(scaler, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            group = scaler.gameObject.AddComponent<CanvasGroup>();

            Image panelImage = UIFactory.CreateImage("Panel", scaler, UIFactory.BarSprite(panelSprite), Color.white);
            panelImage.pixelsPerUnitMultiplier = 1.75f; // thicker green frame, since the bubble is small on screen
            panel = panelImage.rectTransform;
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), Vector2.zero);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)padding.x, (int)padding.x, (int)padding.y, (int)padding.y);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            nameText = UIFactory.CreateText("Name", panel, font, fontSize * 0.7f, Color.white, TextAlignmentOptions.Left);

            body = UIFactory.CreateText("Text", panel, font, fontSize, Color.white, TextAlignmentOptions.Center);
            body.gameObject.AddComponent<LayoutElement>().preferredWidth = maxTextWidth;

            // Shows once a line is finished and waits for the player.
            continueText = UIFactory.CreateText("Continue", panel, font, fontSize * 0.7f, Color.white, TextAlignmentOptions.Right);
            continueText.text = "E";
            continueText.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIFactory.Place(continueText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 6f), new Vector2(40f, 30f));

            typewriter = new Typewriter(body);
        }

        public void Show(SpeakerProfile speaker)
        {
            Build();
            ApplyStyle(speaker);
            if (!visible) group.alpha = 0f;
            scaler.gameObject.SetActive(true);
            visible = true;
            SetContinueVisible(false);
        }

        public void Present(string text, SpeakerProfile speaker, float speedMultiplier = 1f)
        {
            Build();
            float cps = speaker != null ? speaker.charactersPerSecond * speedMultiplier : 30f;
            float pause = speaker != null ? speaker.punctuationPause : 0.15f;
            Typewriter.Begin(text, cps, pause);
            SetContinueVisible(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        public void SetContinueVisible(bool show)
        {
            if (continueText != null) continueText.enabled = show;
        }

        public void Hide()
        {
            visible = false;
            SetContinueVisible(false);
        }

        private void ApplyStyle(SpeakerProfile speaker)
        {
            if (speaker == null) return;
            body.color = speaker.textColor;
            nameText.color = speaker.nameColor;
            continueText.color = speaker.nameColor;
            nameText.text = speaker.displayName;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(speaker.displayName));
        }

        private void LateUpdate()
        {
            if (!built || !scaler.gameObject.activeSelf) return;

            if (mainCamera == null) mainCamera = Camera.main;
            Transform follow = anchor != null ? anchor : transform.parent;
            if (follow != null) transform.position = follow.position + worldOffset;

            if (mainCamera != null)
            {
                Vector3 toBubble = transform.position - mainCamera.transform.position;
                float distance = toBubble.magnitude;
                if (distance > 0.001f) transform.rotation = Quaternion.LookRotation(toBubble / distance, Vector3.up);

                float scaleFactor = Mathf.Clamp(distance / referenceDistance, distanceScaleRange.x, distanceScaleRange.y);
                transform.localScale = Vector3.one * worldScale * scaleFactor;
            }

            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (!visible && group.alpha <= 0f) scaler.gameObject.SetActive(false);
        }
    }
}
