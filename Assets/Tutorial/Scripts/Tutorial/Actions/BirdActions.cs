using System;
using System.Collections;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.Tutorial.Actions
{
    /// <summary>Sends the bird after the player's plants, tuned so the challenge is fair.</summary>
    [Serializable, MenuPath("Bird/Bird Attack")]
    public class BirdAttackAction : TutorialAction
    {
        [Tooltip("TutorialTarget id where the bird appears.")]
        public string spawnTargetId = "birdSpawn";
        [Min(0.05f)] public float searchDelay = 0.3f;
        [Tooltip("Seconds the bird needs to eat. More = easier.")]
        [Min(1f)] public float eatDuration = 7f;
        [Min(1)] public int speed = 4;

        public override IEnumerator Execute(TutorialContext context)
        {
            BirdMovment bird = context.Bird;
            if (bird == null) yield break;

            BirdBackupUtility.EnsureBackup(context);

            Transform spawn = context.ResolveTarget(spawnTargetId);
            if (spawn != null) bird.transform.position = spawn.position;

            bird.searchDelay = searchDelay;
            bird.eatDuration = eatDuration;
            bird.speed = speed;
            bird.currentState = BirdEnum.Idle;
            bird.gameObject.SetActive(true);
        }
    }

    /// <summary>Hides the bird (after an optional delay) and restores its original settings.</summary>
    [Serializable, MenuPath("Bird/Hide Bird")]
    public class HideBirdAction : TutorialAction
    {
        [Min(0f)] public float delay;
        [Tooltip("Run in the background so the tutorial does not wait for the delay.")]
        public bool dontWait = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Bird == null) yield break;
            if (dontWait && delay > 0f)
            {
                context.Run(Hide(context));
                yield break;
            }
            yield return Hide(context);
        }

        private IEnumerator Hide(TutorialContext context)
        {
            BirdBackupUtility.EnsureBackup(context);
            if (delay > 0f) yield return new WaitForSeconds(delay);

            BirdMovment bird = context.Bird;
            if (bird == null) yield break;

            if (context.BirdBackup != null)
            {
                bird.searchDelay = context.BirdBackup.searchDelay;
                bird.eatDuration = context.BirdBackup.eatDuration;
                bird.speed = context.BirdBackup.speed;
            }
            bird.currentState = BirdEnum.Idle;
            bird.gameObject.SetActive(false);
        }
    }

    /// <summary>Puts the bird back how the level had it (for when the tutorial ends).</summary>
    [Serializable, MenuPath("Bird/Restore Bird")]
    public class RestoreBirdAction : TutorialAction
    {
        public override IEnumerator Execute(TutorialContext context)
        {
            BirdMovment bird = context.Bird;
            BirdSettingsBackup backup = context.BirdBackup;
            if (bird == null || backup == null) yield break;
            bird.searchDelay = backup.searchDelay;
            bird.eatDuration = backup.eatDuration;
            bird.speed = backup.speed;
            bird.gameObject.SetActive(backup.wasActive);
        }
    }

    /// <summary>Saves the bird's original settings the first time the tutorial touches it.</summary>
    internal static class BirdBackupUtility
    {
        public static void EnsureBackup(TutorialContext context)
        {
            BirdMovment bird = context.Bird;
            if (bird == null || context.BirdBackup != null) return;
            context.BirdBackup = new BirdSettingsBackup
            {
                searchDelay = bird.searchDelay,
                eatDuration = bird.eatDuration,
                speed = bird.speed,
                wasActive = bird.gameObject.activeSelf
            };
        }
    }
}
