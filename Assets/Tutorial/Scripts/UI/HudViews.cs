using System.Collections;
using System.Collections.Generic;
using GardenGrowers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenGrowers.UI
{
    /// <summary>Top-right panel with the current goal and a box that fills in when it's done.</summary>
    public class ObjectivePanelView
    {
        private readonly HudStyle style;
        private readonly CanvasGroup group;
        private readonly Image checkFill;
        private readonly TextMeshProUGUI goal;
        private readonly TextMeshProUGUI progress;

        private float alphaTarget;
        private bool suppressed;

        public ObjectivePanelView(Transform parent, HudStyle style, string title)
        {
            this.style = style;

            Image panel = UIFactory.CreateImage("Objective", parent, UIFactory.BarSprite(style.panel), Color.white);
            RectTransform root = panel.rectTransform;
            UIFactory.Place(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(520f, 110f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 16, 18);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI header = UIFactory.CreateText("Header", root, style.font, 24f, style.accent);
            header.text = title.ToUpperInvariant();

            RectTransform row = UIFactory.CreateRect("Row", root);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;

            Image checkBox = UIFactory.CreateImage("Check", row, style.slot, Color.white);
            var checkLayout = checkBox.gameObject.AddComponent<LayoutElement>();
            checkLayout.preferredWidth = 36f;
            checkLayout.preferredHeight = 36f;
            checkFill = UIFactory.CreateImage("Fill", checkBox.rectTransform, null, style.done);
            UIFactory.Stretch(checkFill.rectTransform, 9f);

            goal = UIFactory.CreateText("Goal", row, style.font, 32f, style.text);
            var goalLayout = goal.gameObject.AddComponent<LayoutElement>();
            goalLayout.preferredWidth = 420f;
            goalLayout.flexibleWidth = 1f;

            progress = UIFactory.CreateText("Progress", root, style.font, 24f, style.accent);
            progress.gameObject.SetActive(false);
        }

        public void Show(string text)
        {
            goal.text = text;
            goal.color = style.text;
            checkFill.enabled = false;
            SetProgress(null);
            alphaTarget = 1f;
        }

        public void SetProgress(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            progress.gameObject.SetActive(show);
            if (show) progress.text = text;
        }

        /// <summary>Fill the box and strike the goal through.</summary>
        public void Complete()
        {
            goal.text = "<s>" + goal.text + "</s>";
            goal.color = new Color(style.text.r, style.text.g, style.text.b, 0.6f);
            checkFill.enabled = true;
        }

        public void Hide() => alphaTarget = 0f;

        /// <summary>Hide while the group's bag covers the screen, without forgetting the goal.</summary>
        public void SetSuppressed(bool value) => suppressed = value;

        public void Tick(float dt)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, suppressed ? 0f : alphaTarget, dt * 4f);
        }
    }

    /// <summary>Bottom row of control prompts like [Tab] Open your bag.</summary>
    public class HintBarView
    {
        private readonly HudStyle style;
        private readonly RectTransform root;
        private readonly CanvasGroup group;
        private readonly List<(string key, GameObject chip, TextMeshProUGUI label)> chips = new List<(string, GameObject, TextMeshProUGUI)>();
        private float alphaTarget;

        // Above the group's toolbar normally, and under the bag's description panel while the bag is open.
        private static readonly Vector2 NormalPosition = new Vector2(120f, 170f);
        private static readonly Vector2 DockedPosition = new Vector2(120f, 4f);
        private const float DockedScale = 0.82f;

        public HintBarView(Transform parent, HudStyle style)
        {
            this.style = style;
            root = UIFactory.CreateRect("Hints", parent);
            UIFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), NormalPosition, new Vector2(1300f, 64f));
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        public bool IsVisible => alphaTarget > 0f;

        public void Set(IList<ControlHint> hints)
        {
            foreach ((string _, GameObject chip, TextMeshProUGUI _) in chips) Object.Destroy(chip);
            chips.Clear();
            if (hints == null) return;

            foreach (ControlHint hint in hints)
            {
                if (hint == null || string.IsNullOrWhiteSpace(hint.key)) continue;
                TextMeshProUGUI label = BuildChip(hint, out GameObject chip);
                chips.Add((hint.key, chip, label));
            }
        }

        public void Show() => alphaTarget = chips.Count > 0 ? 1f : 0f;
        public void Hide() => alphaTarget = 0f;

        /// <summary>Colour one key orange to draw the eye (for example Tab when the bag is closed).</summary>
        public void Pulse(string key)
        {
            foreach ((string chipKey, GameObject _, TextMeshProUGUI label) in chips)
            {
                bool on = key != null && string.Equals(chipKey, key, System.StringComparison.OrdinalIgnoreCase);
                label.color = on ? style.accent : style.text;
            }
        }

        /// <summary>Move down out of the way while the group's bag is open.</summary>
        public void SetDocked(bool docked)
        {
            root.anchoredPosition = docked ? DockedPosition : NormalPosition;
            root.localScale = Vector3.one * (docked ? DockedScale : 1f);
        }

        public void Tick(float dt)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, alphaTarget, dt * 4f);
        }

        /// <summary>Returns the key label, so Pulse can colour it.</summary>
        private TextMeshProUGUI BuildChip(ControlHint hint, out GameObject chip)
        {
            Image background = UIFactory.CreateImage("Hint " + hint.key, root, UIFactory.BarSprite(style.panel), Color.white);
            chip = background.gameObject;
            var layout = background.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 20, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            Image keyBox = UIFactory.CreateImage("Key", background.rectTransform, style.slot, Color.white);
            TextMeshProUGUI keyText = UIFactory.CreateText("KeyText", keyBox.rectTransform, style.font, 28f, style.text, TextAlignmentOptions.Center);
            keyText.textWrappingMode = TextWrappingModes.NoWrap;
            keyText.text = hint.key;
            UIFactory.Stretch(keyText.rectTransform);

            // The slot art is a big square image, so the box needs its own size or the layout uses the image's.
            var keySize = keyBox.gameObject.AddComponent<LayoutElement>();
            keySize.preferredWidth = Mathf.Max(48f, keyText.GetPreferredValues(hint.key).x + 24f);
            keySize.preferredHeight = 48f;

            TextMeshProUGUI action = UIFactory.CreateText("Action", background.rectTransform, style.font, 28f, style.text);
            action.textWrappingMode = TextWrappingModes.NoWrap;
            action.text = hint.action;

            return keyText;
        }
    }

    /// <summary>"Gogo gave you: Tomato Seeds" pop-up with the item's icon.</summary>
    public class ToastView
    {
        private readonly CanvasGroup group;
        private readonly Image icon;
        private readonly TextMeshProUGUI title;
        private readonly TextMeshProUGUI body;
        private readonly Queue<(string title, string body, Sprite icon)> queue = new Queue<(string, string, Sprite)>();
        private float timer;
        private const float ShowSeconds = 3.2f;

        public ToastView(Transform parent, HudStyle style)
        {
            Image panel = UIFactory.CreateImage("Toast", parent, UIFactory.BarSprite(style.panel), Color.white);
            RectTransform root = panel.rectTransform;
            UIFactory.Place(root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 120f), new Vector2(460f, 130f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Image iconBack = UIFactory.CreateImage("IconBack", root, style.slot, Color.white);
            UIFactory.Place(iconBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(92f, 92f));
            icon = UIFactory.CreateImage("Icon", iconBack.rectTransform, null, Color.white);
            icon.preserveAspect = true;
            UIFactory.Stretch(icon.rectTransform, 14f);

            title = UIFactory.CreateText("Title", root, style.font, 22f, style.accent);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128f, -18f), new Vector2(310f, 30f));

            body = UIFactory.CreateText("Body", root, style.font, 30f, style.text);
            UIFactory.Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128f, -50f), new Vector2(310f, 70f));
        }

        public void Show(string heading, string text, Sprite sprite)
        {
            queue.Enqueue((heading, text, sprite));
            if (timer <= 0f) Next();
        }

        private void Next()
        {
            if (queue.Count == 0) return;
            (string heading, string text, Sprite sprite) = queue.Dequeue();
            title.text = heading;
            body.text = text;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            timer = ShowSeconds;
        }

        public void Tick(float dt)
        {
            if (timer > 0f)
            {
                timer -= dt;
                if (timer <= 0f) Next();
            }

            group.alpha = Mathf.MoveTowards(group.alpha, timer > 0.3f ? 1f : 0f, dt * 5f);
        }
    }

    /// <summary>Full-screen fade to black with an optional caption ("The next morning...").</summary>
    public class ScreenFader
    {
        private readonly Image black;
        private readonly TextMeshProUGUI caption;
        private readonly TextMeshProUGUI subtitle;

        public ScreenFader(Transform parent, HudStyle style)
        {
            black = UIFactory.CreateImage("Fade", parent, null, Color.black);
            UIFactory.Stretch(black.rectTransform);
            UIFactory.SetAlpha(black, 0f);

            caption = UIFactory.CreateText("Caption", black.rectTransform, style.font, 72f, style.text, TextAlignmentOptions.Center);
            UIFactory.Place(caption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1400f, 120f));

            subtitle = UIFactory.CreateText("Subtitle", black.rectTransform, style.font, 36f, style.text, TextAlignmentOptions.Center);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1400f, 80f));

            SetText(null, null);
        }

        public float Alpha => black.color.a;

        public void SetText(string title, string sub)
        {
            caption.text = title ?? string.Empty;
            subtitle.text = sub ?? string.Empty;
        }

        public IEnumerator FadeTo(float alpha, float seconds)
        {
            float start = black.color.a;
            float t = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                Apply(Mathf.Lerp(start, alpha, t / seconds));
                yield return null;
            }
            Apply(alpha);
        }

        private void Apply(float alpha)
        {
            UIFactory.SetAlpha(black, alpha);
            UIFactory.SetAlpha(caption, alpha);
            UIFactory.SetAlpha(subtitle, alpha);
            black.raycastTarget = alpha > 0.01f;
        }
    }

    /// <summary>A big centred card for story moments (the seed tin, the note).</summary>
    public class StoryCardView
    {
        private readonly RectTransform root;
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI title;
        private readonly TextMeshProUGUI body;
        private readonly TextMeshProUGUI footer;
        private readonly Image icon;

        public StoryCardView(Transform parent, HudStyle style)
        {
            Image panel = UIFactory.CreateImage("StoryCard", parent, UIFactory.BarSprite(style.panel), Color.white);
            root = panel.rectTransform;
            UIFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(860f, 500f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            title = UIFactory.CreateText("Title", root, style.font, 52f, style.accent, TextAlignmentOptions.Center);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 70f));

            icon = UIFactory.CreateImage("Icon", root, null, Color.white);
            icon.preserveAspect = true;
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(120f, 120f));

            body = UIFactory.CreateText("Body", root, style.font, 36f, style.text, TextAlignmentOptions.Center);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(740f, 200f));

            footer = UIFactory.CreateText("Footer", root, style.font, 26f, style.accent, TextAlignmentOptions.Center);
            UIFactory.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(740f, 40f));

            root.gameObject.SetActive(false);
        }

        public IEnumerator Show(string heading, string text, Sprite sprite, bool waitForKey, float autoSeconds = 4f)
        {
            title.text = heading;
            body.text = text;
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            body.rectTransform.anchoredPosition = new Vector2(0f, sprite != null ? -60f : 0f);
            footer.text = waitForKey ? "Press E to continue" : string.Empty;
            root.gameObject.SetActive(true);

            yield return FadeTo(1f, 0.25f);

            if (waitForKey)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                var input = Dialogue.DialogueInput.Instance;
                while (!input.PressedThisFrame) yield return null;
                yield return null;
            }
            else
            {
                yield return new WaitForSecondsRealtime(autoSeconds);
            }

            yield return FadeTo(0f, 0.25f);
            root.gameObject.SetActive(false);
        }

        private IEnumerator FadeTo(float alpha, float seconds)
        {
            float start = group.alpha;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, alpha, t / seconds);
                yield return null;
            }
            group.alpha = alpha;
        }
    }
}
