using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GardenGrowers.EditorTools
{
    /// <summary>Loads the group's UI art and the tutorial's own static copy of the Bangers font.</summary>
    public static class TutorialArt
    {
        public const string BarSpritePath = "Assets/UI_Assets/Slot.png";
        public const string SlotSpritePath = "Assets/UI_Assets/Panel.png";
        public const string FontPath = "Assets/Tutorial/Art/Fonts/Bangers Tutorial SDF.asset";
        private const string BangersPath = "Assets/TextMesh Pro/Examples & Extras/Fonts/Bangers.ttf";

        // Every character the tutorial needs. A static font can't add letters later.
        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~‘’“”";

        public static Sprite Bar => AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
        public static Sprite Slot => AssetDatabase.LoadAssetAtPath<Sprite>(SlotSpritePath);

        /// <summary>The tutorial's Bangers font. It gets made the first time it's needed.</summary>
        public static TMP_FontAsset Font()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) font = CreateFont();
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        /// <summary>
        /// The game's Bangers font asset is dynamic, so it rewrites itself when new letters show up.
        /// The tutorial gets its own static copy, so it never changes the shared one.
        /// </summary>
        private static TMP_FontAsset CreateFont()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(BangersPath);
            if (source == null)
            {
                Debug.LogWarning("[Tutorial] Couldn't find " + BangersPath + ", so the tutorial uses the default font.");
                return null;
            }

            GogoBaker.EnsureFolder(System.IO.Path.GetDirectoryName(FontPath).Replace('\\', '/'));
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 72, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            if (font == null) return null;

            font.name = "Bangers Tutorial SDF";
            font.TryAddCharacters(Characters, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log("[Tutorial] Bangers has no letter for: " + missing);

            AssetDatabase.CreateAsset(font, FontPath);
            Texture2D atlas = font.atlasTextures[0];
            atlas.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, font);

            // Same black outline and drop shadow as the game's prompts (Bangers SDF - Drop Shadow).
            Material material = font.material;
            material.name = font.name + " Material";
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat("_OutlineWidth", 0.15f);
            material.SetColor("_OutlineColor", Color.black);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.75f));
            material.SetFloat("_UnderlayOffsetX", 0.29f);
            material.SetFloat("_UnderlayOffsetY", -0.48f);
            material.SetFloat("_UnderlaySoftness", 0.07f);
            AssetDatabase.AddObjectToAsset(material, font);

            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontPath);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }
    }
}
