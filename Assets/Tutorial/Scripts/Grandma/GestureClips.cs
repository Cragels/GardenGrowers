using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>A short one-shot movement (nod, point, hop...) added on top of Gogo's current state.</summary>
    public abstract class GestureClip
    {
        protected GestureClip(float duration)
        {
            Duration = duration;
        }

        public float Duration { get; }
        public float Elapsed { get; private set; }
        public bool IsDone => Elapsed >= Duration;

        /// <summary>0 to 1 through the gesture.</summary>
        protected float T => Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 1f;

        /// <summary>Something the head should look at during this gesture (or null).</summary>
        public virtual Transform LookOverride => null;

        public virtual void Begin(GrandmaAnimator animator) { }
        public void Advance(float deltaTime) => Elapsed += deltaTime;
        public abstract RigPose Evaluate(GrandmaAnimator animator);
        public virtual void End(GrandmaAnimator animator) { }

        /// <summary>Makes the clip for a gesture type. Types with no clip return null and are skipped.</summary>
        public static GestureClip Create(GestureType type, Transform target)
        {
            switch (type)
            {
                case GestureType.Nod: return new NodGesture();
                case GestureType.ShakeHead: return new ShakeHeadGesture();
                case GestureType.Wave: return new WaveGesture();
                case GestureType.Point: return target != null ? new PointGesture(target) : null;
                case GestureType.Hop: return new HopGesture();
                case GestureType.Shoo: return new ShooGesture();
                case GestureType.Shrug: return new ShrugGesture();
                default: return null;
            }
        }
    }

    /// <summary>The happy reaction: a small hop.</summary>
    public class HopGesture : GestureClip
    {
        public HopGesture() : base(0.6f) { }

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            float h = animator.Height;
            float arc = Mathf.Sin(T * Mathf.PI);
            return new RigPose
            {
                bodyOffset = new Vector3(0f, 0.06f * h * arc, 0f),
                freeHandOffset = new Vector3(0f, 0.08f * h * arc, 0.02f * h * arc),
                headEuler = new Vector3(-4f * arc, 0f, 0f)
            };
        }
    }

    /// <summary>Raises her walking stick and points it at the target.</summary>
    public class PointGesture : GestureClip
    {
        private readonly Transform target;

        public PointGesture(Transform target) : base(1.8f)
        {
            this.target = target;
        }

        public override Transform LookOverride => target;

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            float h = animator.Height;
            float weight = Ease.Bell(T, 0.18f, 0.2f);

            if (target != null) animator.SetStickAim(target.position + Vector3.up * 0.4f, weight);

            return new RigPose
            {
                stickHandOffset = new Vector3(0f, 0.12f * h, 0.1f * h) * weight,
                bodyEuler = new Vector3(3f, 0f, 0f) * weight
            };
        }

        public override void End(GrandmaAnimator animator)
        {
            animator.SetStickAim(Vector3.zero, 0f);
        }
    }

    /// <summary>A small wave hello with her free hand.</summary>
    public class WaveGesture : GestureClip
    {
        public WaveGesture() : base(1.2f) { }

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            float h = animator.Height;
            float envelope = Ease.Bell(T, 0.2f, 0.2f);
            float outward = animator.Rig != null ? animator.Rig.OutwardOf(animator.Rig.FreeHandRest).x : 1f;
            return new RigPose
            {
                freeHandOffset = new Vector3(0.05f * h * outward, 0.2f * h, 0.03f * h) * envelope,
                freeHandEuler = new Vector3(0f, 0f, 15f * Mathf.Sin(Elapsed * 9f)) * envelope
            };
        }
    }

    public class NodGesture : GestureClip
    {
        public NodGesture() : base(0.7f) { }

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            return new RigPose { headEuler = new Vector3(8f * Mathf.Sin(T * 4f * Mathf.PI) * Ease.Bell(T, 0.1f, 0.2f), 0f, 0f) };
        }
    }

    public class ShakeHeadGesture : GestureClip
    {
        public ShakeHeadGesture() : base(0.9f) { }

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            return new RigPose { headEuler = new Vector3(3f, 12f * Mathf.Sin(T * 4f * Mathf.PI) * Ease.Bell(T, 0.1f, 0.2f), 0f) };
        }
    }

    /// <summary>"Hayi! Shoo!" Raises her stick and shakes it.</summary>
    public class ShooGesture : GestureClip
    {
        public ShooGesture() : base(1.4f) { }

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            float h = animator.Height;
            float e = Ease.Bell(T, 0.15f, 0.2f);
            return new RigPose
            {
                stickHandOffset = new Vector3(0f, 0.12f * h, 0.06f * h) * e,
                stickEuler = new Vector3(-35f + 10f * Mathf.Sin(Elapsed * 10f), 0f, 0f) * e,
                headEuler = new Vector3(-4f, 0f, 0f) * e
            };
        }
    }

    public class ShrugGesture : GestureClip
    {
        public ShrugGesture() : base(0.9f) { }

        public override RigPose Evaluate(GrandmaAnimator animator)
        {
            float h = animator.Height;
            float e = Ease.Bell(T, 0.25f, 0.3f);
            GrandmaRig rig = animator.Rig;
            Vector3 freeOut = rig != null ? rig.OutwardOf(rig.FreeHandRest) : Vector3.right;
            return new RigPose
            {
                freeHandOffset = (freeOut * 0.04f * h + Vector3.up * 0.03f * h) * e,
                headEuler = new Vector3(0f, 0f, 6f) * e
            };
        }
    }
}
