using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenGrowers.UI
{
    /// <summary>Shows what Gogo says at the bottom of the screen when her speech bubble can't be read.</summary>
    public class SubtitleView
    {
        private const float MaxReadableDistance = 14f;
        private const float MaxTextWidth = 1000f;
        private const float EdgeMargin = 0.03f;

        private readonly HudStyle style;
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI label;
        private readonly LayoutElement labelLayout;

        private readonly Vector3[] corners = new Vector3[4];
        private DialogueSpeaker speaker;
        private Camera cam;
        private string shownText;
        private bool suppressed;

        public SubtitleView(Transform parent, HudStyle style)
        {
            this.style = style;

            Image panel = UIFactory.CreateImage("Subtitle", parent, UIFactory.BarSprite(style.panel), Color.white);
            RectTransform root = panel.rectTransform;
            UIFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 262f), new Vector2(MaxTextWidth, 64f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 12, 14);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = UIFactory.CreateText("Text", root, style.font, 32f, style.text, TextAlignmentOptions.Center);
            labelLayout = label.gameObject.AddComponent<LayoutElement>();
        }

        public void Watch(DialogueSpeaker newSpeaker) => speaker = newSpeaker;

        /// <summary>Hide while the group's bag covers the screen.</summary>
        public void SetSuppressed(bool value) => suppressed = value;

        public void Tick(float dt)
        {
            if (cam == null) cam = Camera.main;

            bool show = !suppressed && speaker != null && speaker.IsSpeaking
                        && !string.IsNullOrEmpty(speaker.CurrentText) && !BubbleReadable();
            if (show && speaker.CurrentText != shownText) SetText(speaker.CurrentText);
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, dt * 5f);
        }

        private void SetText(string text)
        {
            shownText = text;
            string name = speaker.Profile != null ? speaker.Profile.displayName : "";
            string colour = ColorUtility.ToHtmlStringRGB(style.accent);
            label.text = string.IsNullOrEmpty(name) ? text : "<color=#" + colour + ">" + name + ":</color> " + text;

            float width = label.GetPreferredValues(label.text).x;
            labelLayout.preferredWidth = Mathf.Min(width + 2f, MaxTextWidth);
        }

        /// <summary>True when the whole bubble is on screen, close enough and not behind a wall.</summary>
        private bool BubbleReadable()
        {
            if (cam == null) return true;

            RectTransform panel = speaker.Bubble != null ? speaker.Bubble.Panel : null;
            if (panel == null) return PointReadable(speaker.BubbleTransform.position);

            panel.GetWorldCorners(corners); // bottom-left, top-left, top-right, bottom-right
            foreach (Vector3 corner in corners)
            {
                if (!OnScreen(cam.WorldToViewportPoint(corner), 0f)) return false;
            }

            Vector3 left = (corners[0] + corners[1]) * 0.5f;
            Vector3 right = (corners[2] + corners[3]) * 0.5f;
            return PointReadable((left + right) * 0.5f) && PointReadable(Vector3.Lerp(left, right, 0.1f)) && PointReadable(Vector3.Lerp(left, right, 0.9f));
        }

        private static bool OnScreen(Vector3 viewport, float margin)
        {
            return viewport.z > 0f && viewport.x >= margin && viewport.x <= 1f - margin && viewport.y >= margin && viewport.y <= 1f - margin;
        }

        private bool PointReadable(Vector3 point)
        {
            Vector3 viewport = cam.WorldToViewportPoint(point);
            if (viewport.z > MaxReadableDistance || !OnScreen(viewport, EdgeMargin)) return false;

            // A wall or fence between the camera and the bubble?
            if (Physics.Linecast(cam.transform.position, point, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(speaker.transform)) return false;
            }
            return true;
        }
    }
}
