using GardenGrowers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenGrowers.UI
{
    /// <summary>Marks the current target on screen with its distance, or sits on the screen edge when it's off screen.</summary>
    public class ObjectiveMarker
    {
        private const float EdgeMargin = 70f;
        private const float TopMargin = 175f; // at 1080p, keeps it under the Money and goal panels

        private readonly RectTransform root;
        private readonly RectTransform arrow;
        private readonly TextMeshProUGUI distanceLabel;
        private readonly CanvasGroup group;

        private Transform target;
        private float heightOffset;
        private Camera cam;
        private bool suppressed;

        public Transform Target => target;

        public ObjectiveMarker(Transform parent, HudStyle style)
        {
            root = UIFactory.CreateRect("Marker", parent);
            UIFactory.Place(root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Image outline = UIFactory.CreateImage("Outline", root, null, new Color(0.28f, 0.14f, 0f));
            UIFactory.Place(outline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            outline.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image fill = UIFactory.CreateImage("Fill", outline.rectTransform, null, style.accent);
            UIFactory.Stretch(fill.rectTransform, 5f);

            Image arrowImage = UIFactory.CreateImage("Arrow", root, null, style.accent);
            arrow = arrowImage.rectTransform;
            UIFactory.Place(arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            arrow.gameObject.SetActive(false);

            distanceLabel = UIFactory.CreateText("Distance", root, style.font, 26f, style.text, TextAlignmentOptions.Center);
            UIFactory.Place(distanceLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(140f, 34f));
        }

        public void Track(Transform newTarget, float height)
        {
            target = newTarget;
            heightOffset = height;
        }

        public void Clear() => target = null;

        /// <summary>Hide (but keep tracking) while the bag covers the screen.</summary>
        public void SetSuppressed(bool value) => suppressed = value;

        public void Tick(float dt)
        {
            if (cam == null) cam = Camera.main;
            bool active = target != null && cam != null;
            if (!active)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, dt * 6f);
                return;
            }

            Vector3 world = target.position + Vector3.up * heightOffset;
            Vector3 screen = cam.WorldToScreenPoint(world);
            bool behind = screen.z < 0f;
            if (behind)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }

            float topMargin = TopMargin * Screen.height / 1080f;
            Vector2 min = new Vector2(EdgeMargin, EdgeMargin);
            Vector2 max = new Vector2(Screen.width - EdgeMargin, Screen.height - Mathf.Max(EdgeMargin, topMargin));
            Vector2 position = screen;
            bool onScreen = !behind && position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y;

            if (!onScreen)
            {
                // Push it to the screen edge in the target's direction, with an arrow pointing the way.
                Vector2 centre = new Vector2(Screen.width, Screen.height) * 0.5f;
                Vector2 direction = position - centre;
                if (behind && direction.y > 0f) direction.y = -direction.y;
                if (direction.sqrMagnitude < 1f) direction = Vector2.down;

                float limitX = direction.x >= 0f ? max.x - centre.x : centre.x - min.x;
                float limitY = direction.y >= 0f ? max.y - centre.y : centre.y - min.y;
                float sx = Mathf.Abs(direction.x) > 0.001f ? limitX / Mathf.Abs(direction.x) : float.MaxValue;
                float sy = Mathf.Abs(direction.y) > 0.001f ? limitY / Mathf.Abs(direction.y) : float.MaxValue;
                position = centre + direction * Mathf.Min(sx, sy);

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                arrow.gameObject.SetActive(true);
                arrow.localPosition = (Vector3)(direction.normalized * 36f);
                arrow.localRotation = Quaternion.Euler(0f, 0f, angle + 45f);
            }
            else
            {
                arrow.gameObject.SetActive(false);
            }

            root.position = new Vector3(position.x, position.y, 0f);

            float metres = Vector3.Distance(cam.transform.position, target.position);
            distanceLabel.text = metres > 3f ? Mathf.RoundToInt(metres) + "m" : string.Empty;

            // Right next to it, the group's own prompt takes over, so get out of its way.
            bool close = onScreen && metres < 3f;
            group.alpha = Mathf.MoveTowards(group.alpha, suppressed || close ? 0f : 1f, dt * 6f);
        }
    }

    /// <summary>Draws a frame around the bag slot that holds a given item, so the player knows what to click.</summary>
    public class BagSlotHighlighter
    {
        private readonly RectTransform frame;
        private readonly Vector3[] corners = new Vector3[4];
        private readonly Canvas canvas;
        private ItemData item;

        public BagSlotHighlighter(Transform parent, HudStyle style, Canvas canvas)
        {
            this.canvas = canvas;
            frame = UIFactory.CreateRect("BagHighlight", parent);
            UIFactory.Place(frame, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));

            // Four thin bars make the frame.
            const float thickness = 6f;
            AddEdge(style, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness));
            AddEdge(style, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness));
            AddEdge(style, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f));
            AddEdge(style, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f));
            frame.gameObject.SetActive(false);
        }

        private void AddEdge(HudStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            Image edge = UIFactory.CreateImage("Edge", frame, null, style.accent);
            RectTransform rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        public void Highlight(ItemData newItem) => item = newItem;
        public void Clear() => item = null;

        /// <summary>True while the group's bag panel is open.</summary>
        public static bool IsBagOpen
        {
            get
            {
                UIManager ui = UIManager.Instance;
                return ui != null && ui.toolSlots != null && ui.toolSlots.Length > 0 && ui.toolSlots[0] != null
                       && ui.toolSlots[0].gameObject.activeInHierarchy;
            }
        }

        public void Tick()
        {
            RectTransform slot = FindSlot();
            frame.gameObject.SetActive(slot != null);
            if (slot == null) return;

            slot.GetWorldCorners(corners);
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            frame.position = (corners[0] + corners[2]) * 0.5f;
            frame.sizeDelta = (Vector2)(corners[2] - corners[0]) / Mathf.Max(0.01f, scale) + new Vector2(14f, 14f);
        }

        private RectTransform FindSlot()
        {
            InventoryManager inventory = InventoryManager.Instance;
            UIManager ui = UIManager.Instance;
            if (item == null || inventory == null || ui == null || !IsBagOpen) return null;
            if (inventory.equippedTool == item || inventory.equippedItem == item) return null; // already holding it

            int index = System.Array.IndexOf(inventory.tools, item);
            if (index >= 0 && ui.toolSlots != null && index < ui.toolSlots.Length) return ui.toolSlots[index].transform as RectTransform;

            index = System.Array.IndexOf(inventory.items, item);
            if (index >= 0 && ui.itemSlots != null && index < ui.itemSlots.Length) return ui.itemSlots[index].transform as RectTransform;

            return null;
        }
    }
}
