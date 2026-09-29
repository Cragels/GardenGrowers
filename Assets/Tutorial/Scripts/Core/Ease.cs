using UnityEngine;

namespace GardenGrowers.Core
{
    /// <summary>Easing helpers for animation and UI tweens.</summary>
    public static class Ease
    {
        public static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * t + 1f;
        }

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;

        /// <summary>Overshoots a little, then settles. Nice for pop-ins.</summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        /// <summary>0 at the edges, 1 in the middle. Handy for fading a gesture in and out.</summary>
        public static float Bell(float t, float fadeIn = 0.2f, float fadeOut = 0.2f)
        {
            t = Mathf.Clamp01(t);
            if (t < fadeIn) return Smooth(t / Mathf.Max(0.0001f, fadeIn));
            if (t > 1f - fadeOut) return Smooth((1f - t) / Mathf.Max(0.0001f, fadeOut));
            return 1f;
        }

        /// <summary>Lerp amount for smooth follow that works the same at any frame rate.</summary>
        public static float Damp(float sharpness, float deltaTime) => 1f - Mathf.Exp(-sharpness * deltaTime);
    }
}
