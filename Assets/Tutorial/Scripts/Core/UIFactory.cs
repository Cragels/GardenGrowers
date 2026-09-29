using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenGrowers.Core
{
    /// <summary>Small helpers for building the tutorial UI from code.</summary>
    public static class UIFactory
    {
        public const int UILayer = 5;

        // Makes sliced borders thinner, so they match the group's HUD.
        private const float BorderScale = 2f;

        private static readonly Dictionary<Sprite, Sprite> bars = new Dictionary<Sprite, Sprite>();

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : UILayer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = BorderScale;
            }
            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        /// <summary>
        /// The group's bar art (Slot.png) sits in the middle of a big empty image and has no 9-slice border.
        /// This cuts out just the bar and adds a border, so it can stretch to any panel size.
        /// </summary>
        public static Sprite BarSprite(Sprite source)
        {
            if (source == null) return null;
            if (bars.TryGetValue(source, out Sprite cached) && cached != null) return cached;

            // rect, not textureRect: textureRect skips the see-through edges and would cut off the green frame.
            Rect r = source.rect;
            var area = new Rect(r.x + r.width * 0.045f, r.y + r.height * 0.34f, r.width * 0.927f, r.height * 0.236f);
            float border = r.width * 0.035f;
            Sprite bar = Sprite.Create(source.texture, area, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            bar.name = source.name + " Bar";
            bars[source] = bar;
            return bar;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Anchor to one point of the parent (0,0 = bottom left, 1,1 = top right).</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static Canvas CreateScreenCanvas(string name, Transform parent, int sortingOrder, Vector2 referenceResolution)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
