using System;
using System.Collections;
using GardenGrowers.Core;
using GardenGrowers.FX;
using UnityEngine;

namespace GardenGrowers.Tutorial.Actions
{
    /// <summary>Sets how fast the game clock runs. The tutorial slows it so it stays daytime.</summary>
    [Serializable, MenuPath("Time/Set Clock Speed")]
    public class SetClockSpeedAction : TutorialAction
    {
        [Min(0.1f)] public float gameMinutesPerSecond = 1f;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (TimeManager.Instance != null) TimeManager.Instance.timeScale = gameMinutesPerSecond;
            yield break;
        }
    }

    /// <summary>Puts the clock back to the speed it had before the tutorial.</summary>
    [Serializable, MenuPath("Time/Restore Clock Speed")]
    public class RestoreClockSpeedAction : TutorialAction
    {
        public override IEnumerator Execute(TutorialContext context)
        {
            if (TimeManager.Instance != null) TimeManager.Instance.timeScale = context.OriginalClockSpeed;
            yield break;
        }
    }

    /// <summary>
    /// Runs the clock forward to a set time over a few seconds, so the sun sweeps across the sky.
    /// Uses TimeManager.Tick(), so plots, the HUD clock and the sun update as normal.
    /// </summary>
    [Serializable, MenuPath("Time/Time-lapse To Hour")]
    public class TimeLapseAction : TutorialAction
    {
        [Range(0, 23)] public int hour = 18;
        [Range(0, 59)] public int minute = 5;
        [Min(0.1f)] public float realSeconds = 5f;
        [Tooltip("Optional banner shown during the time-lapse.")]
        public string banner = "";
        [Tooltip("Off = the tutorial carries on while the sun moves (for example while walking home).")]
        public bool waitUntilDone = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (waitUntilDone) yield return Lapse(context);
            else context.Run(Lapse(context));
        }

        private IEnumerator Lapse(TutorialContext context)
        {
            TimeManager clock = TimeManager.Instance;
            if (clock == null) yield break;

            GameTimeStamp now = clock.GetGameTimeStamp();
            int minutesToGo = (hour * 60 + minute) - (now.hour * 60 + now.minute);
            if (minutesToGo <= 0) yield break; // already past that time today

            if (!string.IsNullOrEmpty(banner) && context.Hud != null) context.Hud.ShowBanner(banner, realSeconds);
            if (context.Fx != null) context.Fx.Play(Sfx.Whoosh, 0.6f);

            float elapsed = 0f;
            int done = 0;
            while (done < minutesToGo)
            {
                elapsed += Time.deltaTime;
                int shouldBeDone = Mathf.Min(minutesToGo, Mathf.CeilToInt(minutesToGo * Ease.InOutSine(elapsed / realSeconds)));
                for (; done < shouldBeDone; done++) clock.Tick();
                yield return null;
            }
        }
    }
}
