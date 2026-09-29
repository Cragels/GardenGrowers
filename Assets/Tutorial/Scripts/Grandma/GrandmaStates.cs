using GardenGrowers.Dialogue;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>Base class for Gogo's animation states. Each state makes a pose every frame.</summary>
    public abstract class GrandmaState
    {
        protected readonly GrandmaAnimator Animator;

        protected GrandmaState(GrandmaAnimator animator)
        {
            Animator = animator;
        }

        public virtual string Name => GetType().Name;
        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract RigPose Evaluate(float deltaTime);

        protected static float Wave(float time, float period, float offset = 0f)
        {
            return Mathf.Sin((time / period + offset) * 2f * Mathf.PI);
        }
    }

    /// <summary>Standing still with slow breathing and a little sway.</summary>
    public class GrandmaIdleState : GrandmaState
    {
        public GrandmaIdleState(GrandmaAnimator animator) : base(animator) { }

        public override RigPose Evaluate(float deltaTime)
        {
            float t = Animator.LocalTime;
            float h = Animator.Height;
            float breath = Wave(t, 3.2f);

            return new RigPose
            {
                squash = 0.006f * breath,
                bodyEuler = new Vector3(0.5f * breath, 0f, 0.8f * Wave(t, 5.3f)),
                headEuler = new Vector3(1f * Wave(t, 4.1f, 0.2f), 0f, 1.2f * Wave(t, 6.7f)),
                freeHandOffset = new Vector3(0f, 0.01f * h * Wave(t, 2.4f), 0f),
                stickHandOffset = new Vector3(0f, 0.005f * h * Wave(t, 2.9f, 0.3f), 0f)
            };
        }
    }

    /// <summary>A gentle waddle: small bob and roll each step, and the stick swings with her.</summary>
    public class GrandmaWalkState : GrandmaState
    {
        private float phase;

        public GrandmaWalkState(GrandmaAnimator animator) : base(animator) { }

        public override void Enter()
        {
            phase = 0.15f;
        }

        public override RigPose Evaluate(float deltaTime)
        {
            GrandmaAnimationSettings s = Animator.Settings;
            float h = Animator.Height;
            float speed = Animator.LocomotionSpeed;

            // Hurrying makes the steps longer, not just faster, so she never looks frantic.
            float hurry = Mathf.InverseLerp(s.walkSpeedReference, s.hurrySpeedReference, speed);
            float stepLength = Mathf.Max(0.05f, s.stepLength * h * (1f + 1.2f * hurry));
            phase += Mathf.Max(speed, 0.6f) / stepLength * deltaTime;

            int step = Mathf.FloorToInt(phase);
            float p = phase - step;
            float side = step % 2 == 0 ? 1f : -1f;

            float arc = Mathf.Sin(p * Mathf.PI);
            float bob = Mathf.Lerp(s.hopHeight, s.hurryHopHeight, hurry) * h;
            float lean = Mathf.Min(s.leanPerSpeed * speed, s.maxLean);
            float swing = Mathf.Cos(p * Mathf.PI);

            return new RigPose
            {
                bodyOffset = new Vector3(0f, bob * arc, 0f),
                bodyEuler = new Vector3(lean, s.twist * side * arc, s.waddle * side * arc),

                // Keep the head fairly level so she still looks where she's going.
                headEuler = new Vector3(-0.5f * lean, -0.5f * s.twist * side * arc, -0.5f * s.waddle * side * arc),

                freeHandOffset = new Vector3(0f, 0.015f * h * arc, 0.04f * h * side * swing),
                freeHandEuler = new Vector3(-10f * side * swing, 0f, 0f),
                stickHandOffset = new Vector3(0f, 0.02f * h * arc, -0.03f * h * side * swing),
                stickEuler = new Vector3(6f * side * swing, 0f, 0f)
            };
        }
    }

    /// <summary>Talking: a calmer idle plus her mood and a little hand movement.</summary>
    public class GrandmaTalkState : GrandmaState
    {
        private readonly GrandmaIdleState idle;

        public GrandmaTalkState(GrandmaAnimator animator, GrandmaIdleState idle) : base(animator)
        {
            this.idle = idle;
        }

        public override RigPose Evaluate(float deltaTime)
        {
            float t = Animator.LocalTime;
            float h = Animator.Height;
            float energy = Animator.TalkEnergy;

            RigPose pose = idle.Evaluate(deltaTime) * 0.7f;
            pose += Animator.MoodPose;
            pose.freeHandOffset += new Vector3(
                0.01f * h * Mathf.Sin(t * 3.1f),
                0.015f * h * Mathf.Sin(t * 3.7f + 1f),
                0f) * energy;
            return pose;
        }
    }

    /// <summary>Small body language changes for each mood while she talks.</summary>
    public static class GrandmaMoodPoses
    {
        public static RigPose For(Mood mood, GrandmaAnimator animator)
        {
            GrandmaRig rig = animator.Rig;
            float h = animator.Height;
            var pose = new RigPose();

            switch (mood)
            {
                case Mood.Happy:
                case Mood.Excited:
                    pose.headEuler = new Vector3(-3f, 0f, 4f);
                    pose.freeHandOffset = new Vector3(0.02f * h * Outward(rig), 0.04f * h, 0.01f * h);
                    break;

                case Mood.Proud:
                    pose.bodyEuler = new Vector3(-3f, 0f, 0f);
                    pose.headEuler = new Vector3(-5f, 0f, 0f);
                    break;

                case Mood.Thinking:
                    pose.headEuler = new Vector3(-4f, 0f, 7f);
                    pose.freeHandOffset = TowardFace(rig, h, 0.6f);
                    break;

                case Mood.Worried:
                    pose.headEuler = new Vector3(5f, 0f, -3f);
                    pose.bodyEuler = new Vector3(2f, 0f, 0f);
                    break;

                case Mood.Sad:
                    pose.bodyEuler = new Vector3(4f, 0f, 0f);
                    pose.headEuler = new Vector3(9f, 0f, 2f);
                    pose.freeHandOffset = new Vector3(0f, -0.02f * h, 0f);
                    break;
            }

            return pose;
        }

        private static float Outward(GrandmaRig rig)
        {
            if (rig == null) return 1f;
            return rig.OutwardOf(rig.FreeHandRest).x;
        }

        /// <summary>Offset that brings the free hand up near her chin.</summary>
        private static Vector3 TowardFace(GrandmaRig rig, float h, float amount)
        {
            if (rig == null || !rig.FreeHandRest.valid || !rig.HeadRest.valid) return new Vector3(0f, 0.1f * h, 0.05f * h);
            Vector3 chin = rig.HeadRest.position + new Vector3(0f, 0.02f * h, 0.14f * h);
            return (chin - rig.FreeHandRest.position) * amount;
        }
    }
}
