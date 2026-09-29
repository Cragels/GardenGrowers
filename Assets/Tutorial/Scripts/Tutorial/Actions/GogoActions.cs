using System;
using System.Collections;
using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using UnityEngine;

namespace GardenGrowers.Tutorial.Actions
{
    /// <summary>Gogo says something. Can wait for her to finish or carry on straight away.</summary>
    [Serializable, MenuPath("Gogo/Say")]
    public class GogoSayAction : TutorialAction
    {
        public DialogueSequence lines = new DialogueSequence();
        public bool waitUntilFinished = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Gogo == null || lines == null || lines.IsEmpty) yield break;
            if (waitUntilFinished) yield return context.Gogo.Say(lines);
            else context.Gogo.Comment(lines);
        }
    }

    /// <summary>Gogo does a gesture (hop, clap, wave...). Point uses the step's target.</summary>
    [Serializable, MenuPath("Gogo/Gesture")]
    public class GogoGestureAction : TutorialAction
    {
        public GestureType gesture = GestureType.Hop;
        [Min(0f)] public float waitSeconds;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Gogo == null) yield break;
            if (gesture == GestureType.Point && context.Gogo.PointTarget != null) context.Run(context.Gogo.PointAt(context.Gogo.PointTarget));
            else context.Gogo.Gesture(gesture);
            if (waitSeconds > 0f) yield return new WaitForSeconds(waitSeconds);
        }
    }

    /// <summary>Gogo's happy hop and a chime.</summary>
    [Serializable, MenuPath("Gogo/Celebrate")]
    public class CelebrateAction : TutorialAction
    {
        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Gogo != null) context.Gogo.Celebrate();
            if (context.Fx != null) context.Fx.Celebrate();
            yield break;
        }
    }

    /// <summary>Gogo walks to a target.</summary>
    [Serializable, MenuPath("Gogo/Walk To")]
    public class GogoWalkToAction : TutorialAction
    {
        public string targetId = "";
        [Min(0.1f)] public float stopDistance = 0.5f;
        public bool waitUntilArrived;
        [Min(1f)] public float timeout = 25f;

        public override IEnumerator Execute(TutorialContext context)
        {
            Transform target = context.ResolveTarget(targetId);
            if (context.Gogo == null || target == null) yield break;
            if (waitUntilArrived) yield return context.Gogo.WalkTo(target.position, stopDistance, timeout);
            else context.Gogo.GoTo(target.position, stopDistance);
        }
    }

    /// <summary>Moves Gogo instantly (for scene cuts behind a fade).</summary>
    [Serializable, MenuPath("Gogo/Teleport To")]
    public class GogoTeleportAction : TutorialAction
    {
        public string targetId = "";

        public override IEnumerator Execute(TutorialContext context)
        {
            Transform target = context.ResolveTarget(targetId);
            if (context.Gogo == null || target == null) yield break;
            context.Gogo.Teleport(target.position, target.rotation);
            yield break;
        }
    }

    /// <summary>Tells Gogo to follow the player again.</summary>
    [Serializable, MenuPath("Gogo/Follow Player")]
    public class GogoFollowAction : TutorialAction
    {
        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Gogo != null) context.Gogo.FollowPlayer();
            yield break;
        }
    }
}
