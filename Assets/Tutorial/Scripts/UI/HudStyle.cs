using System;
using TMPro;
using UnityEngine;

namespace GardenGrowers.UI
{
    /// <summary>The font, art and colours the tutorial UI shares, taken from the group's HUD.</summary>
    [Serializable]
    public class HudStyle
    {
        public TMP_FontAsset font;
        [Tooltip("The group's bar art (UI_Assets/Slot.png), used behind panels.")]
        public Sprite panel;
        [Tooltip("The group's slot art (UI_Assets/Panel.png), used for key and icon squares.")]
        public Sprite slot;

        public Color text = Color.white;
        public Color accent = new Color(1f, 0.61f, 0f);
        public Color done = new Color(0.36f, 0.62f, 0.2f);
    }

    /// <summary>A control prompt on the HUD, like [Tab] Open your bag.</summary>
    [Serializable]
    public class ControlHint
    {
        [Tooltip("The key, e.g. E, Tab, WASD, Mouse, Shift.")]
        public string key;
        [Tooltip("What it does, e.g. Pick up.")]
        public string action;

        public ControlHint()
        {
        }

        public ControlHint(string key, string action)
        {
            this.key = key;
            this.action = action;
        }
    }
}
