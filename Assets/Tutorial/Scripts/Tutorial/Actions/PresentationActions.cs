using System;
using System.Collections;
using GardenGrowers.CameraSystems;
using GardenGrowers.Core;
using GardenGrowers.FX;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GardenGrowers.Tutorial.Actions
{
    /// <summary>Waits a few seconds.</summary>
    [Serializable, MenuPath("Flow/Wait")]
    public class WaitAction : TutorialAction
    {
        [Min(0f)] public float seconds = 1f;

        public override IEnumerator Execute(TutorialContext context)
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    /// <summary>Fades the screen to black (or back), with an optional caption.</summary>
    [Serializable, MenuPath("Screen/Fade")]
    public class FadeAction : TutorialAction
    {
        public bool toBlack = true;
        [Min(0.01f)] public float seconds = 1f;
        public string caption = "";
        public string subtitle = "";

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Hud == null) yield break;
            yield return context.Hud.Fade(toBlack ? 1f : 0f, seconds,
                string.IsNullOrEmpty(caption) ? null : caption,
                string.IsNullOrEmpty(subtitle) ? null : subtitle);
        }
    }

    /// <summary>Quick fade out and in with a caption, like "The next morning...".</summary>
    [Serializable, MenuPath("Screen/Fade Flash")]
    public class FadeFlashAction : TutorialAction
    {
        public string caption = "The next morning...";
        [Min(0.01f)] public float fadeInSeconds = 0.15f;
        [Min(0f)] public float holdSeconds = 1.6f;
        [Min(0.01f)] public float fadeOutSeconds = 1f;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Hud == null) yield break;
            yield return context.Hud.Fade(1f, fadeInSeconds, caption, "");
            yield return new WaitForSeconds(holdSeconds);
            yield return context.Hud.Fade(0f, fadeOutSeconds);
        }
    }

    /// <summary>A big centred banner, like "NEW GOAL".</summary>
    [Serializable, MenuPath("Screen/Banner")]
    public class BannerAction : TutorialAction
    {
        public string text = "";
        [Min(0.5f)] public float seconds = 3.5f;
        public bool playSound = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Hud != null) context.Hud.ShowBanner(text, seconds);
            if (playSound && context.Fx != null) context.Fx.Play(Sfx.Reward, 0.7f);
            yield break;
        }
    }

    /// <summary>A card for story moments (the seed tin, Mkhulu's note).</summary>
    [Serializable, MenuPath("Screen/Story Card")]
    public class StoryCardAction : TutorialAction
    {
        public string title = "";
        [TextArea(2, 5)] public string body = "";
        public Sprite icon;
        public bool waitForKey = true;
        [Min(0.5f)] public float autoSeconds = 4f;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Hud == null) yield break;
            yield return context.Hud.ShowStory(title, body, icon, waitForKey, autoSeconds);
        }
    }

    /// <summary>Plays one of the built-in sounds.</summary>
    [Serializable, MenuPath("Screen/Play Sound")]
    public class PlaySoundAction : TutorialAction
    {
        public Sfx sound = Sfx.Chime;
        [Range(0f, 1f)] public float volume = 1f;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Fx != null) context.Fx.Play(sound, volume);
            yield break;
        }
    }

    /// <summary>Turns a TutorialTarget's object on or off (for example Gogo's sell counter).</summary>
    [Serializable, MenuPath("World/Set Target Active")]
    public class SetTargetActiveAction : TutorialAction
    {
        public string targetId = "";
        public bool active = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            TutorialTarget target = TutorialTarget.Find(targetId, true);
            if (target != null) target.gameObject.SetActive(active);
            else Debug.LogWarning("[Tutorial] No TutorialTarget called '" + targetId + "' in the scene.");
            yield break;
        }
    }

    /// <summary>Points the player's camera at something for a moment (a short cut-scene look).</summary>
    [Serializable, MenuPath("Camera/Look At")]
    public class CameraLookAtAction : TutorialAction
    {
        public string targetId = "";
        [Min(0.2f)] public float seconds = 1.5f;

        public override IEnumerator Execute(TutorialContext context)
        {
            Transform target = context.ResolveTarget(targetId);
            if (target == null || CameraFocus.Instance == null) yield break;
            yield return CameraFocus.Instance.LookAtFor(target, seconds);
        }
    }

    /// <summary>Takes away (or gives back) player control, for cut-scenes.</summary>
    [Serializable, MenuPath("Camera/Lock Player")]
    public class LockPlayerAction : TutorialAction
    {
        public bool locked = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (PlayerControlLock.Instance == null) yield break;
            if (locked) PlayerControlLock.Instance.Lock(context.Manager);
            else PlayerControlLock.Instance.Unlock(context.Manager);
        }
    }

    /// <summary>The end: fades to a title card, waits for a key, then loads the next scene.</summary>
    [Serializable, MenuPath("Flow/End Tutorial")]
    public class EndTutorialAction : TutorialAction
    {
        public string title = "GARDEN GROWERS";
        public string subtitle = "To be continued...";
        [Tooltip("Scene to load afterwards. Must be in Build Settings. Empty = stay here.")]
        public string nextScene = "MainMenu";

        public override IEnumerator Execute(TutorialContext context)
        {
            if (context.Hud != null)
            {
                yield return context.Hud.Fade(1f, 1.5f, title, subtitle);
                if (context.Fx != null) context.Fx.Play(Sfx.Reward, 0.9f);
                yield return new WaitForSecondsRealtime(2.5f);

                var input = Dialogue.DialogueInput.Instance;
                context.Hud.SetFadeText(title, subtitle + "\n\n<size=60%>Press E to continue</size>");
                while (!input.PressedThisFrame) yield return null;
            }

            if (PlayerControlLock.Instance != null) PlayerControlLock.Instance.UnlockAll();

            if (!string.IsNullOrWhiteSpace(nextScene))
            {
                // The first-person controller locked the cursor. Menus need it back.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 1f;
                SceneManager.LoadScene(nextScene);
            }
            else if (context.Hud != null)
            {
                yield return context.Hud.Fade(0f, 1f);
            }
        }
    }
}
