using UnityEngine;

namespace GardenGrowers.Core
{
    /// <summary>
    /// How a spring feels. Frequency is how fast it reacts (in Hz).
    /// Damping 1 = settles with no wobble, below 1 = bouncy, above 1 = sluggish.
    /// </summary>
    [System.Serializable]
    public struct SpringSettings
    {
        [Min(0.01f)] public float frequency;
        [Range(0f, 2f)] public float damping;

        public SpringSettings(float frequency, float damping)
        {
            this.frequency = frequency;
            this.damping = damping;
        }
    }

    /// <summary>Shared spring maths. Uses small fixed steps so a laggy frame can't make a spring blow up.</summary>
    public static class SpringMath
    {
        private const float SubStep = 1f / 120f;
        private const float MaxDelta = 0.1f;

        public static void Step(ref float value, ref float velocity, float target, SpringSettings settings, float deltaTime)
        {
            float omega = 2f * Mathf.PI * Mathf.Max(0.01f, settings.frequency);
            float remaining = Mathf.Min(deltaTime, MaxDelta);
            while (remaining > 0f)
            {
                float h = Mathf.Min(remaining, SubStep);
                float acceleration = -omega * omega * (value - target) - 2f * settings.damping * omega * velocity;
                velocity += acceleration * h;
                value += velocity * h;
                remaining -= h;
            }
        }

        public static void Step(ref Vector3 value, ref Vector3 velocity, Vector3 target, SpringSettings settings, float deltaTime)
        {
            float omega = 2f * Mathf.PI * Mathf.Max(0.01f, settings.frequency);
            float remaining = Mathf.Min(deltaTime, MaxDelta);
            while (remaining > 0f)
            {
                float h = Mathf.Min(remaining, SubStep);
                Vector3 acceleration = -omega * omega * (value - target) - 2f * settings.damping * omega * velocity;
                velocity += acceleration * h;
                value += velocity * h;
                remaining -= h;
            }
        }
    }

    /// <summary>A single number that springs toward a target.</summary>
    public class SpringFloat
    {
        public float Value;
        public float Velocity;

        public SpringFloat(float start = 0f)
        {
            Value = start;
        }

        public float Step(float target, SpringSettings settings, float deltaTime)
        {
            SpringMath.Step(ref Value, ref Velocity, target, settings, deltaTime);
            return Value;
        }

        public void AddImpulse(float impulse) => Velocity += impulse;

        public void Reset(float value)
        {
            Value = value;
            Velocity = 0f;
        }
    }

    /// <summary>A Vector3 that springs toward a target.</summary>
    public class SpringVector3
    {
        public Vector3 Value;
        public Vector3 Velocity;

        public SpringVector3(Vector3 start = default)
        {
            Value = start;
        }

        public Vector3 Step(Vector3 target, SpringSettings settings, float deltaTime)
        {
            SpringMath.Step(ref Value, ref Velocity, target, settings, deltaTime);
            return Value;
        }

        public void AddImpulse(Vector3 impulse) => Velocity += impulse;

        public void Reset(Vector3 value)
        {
            Value = value;
            Velocity = Vector3.zero;
        }
    }
}
