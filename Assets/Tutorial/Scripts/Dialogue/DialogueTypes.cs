using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenGrowers.Dialogue
{
    /// <summary>How the speaker feels on a line. Drives voice pitch and body language.</summary>
    public enum Mood
    {
        Neutral,
        Happy,
        Excited,
        Proud,
        Thinking,
        Worried,
        Sad
    }

    /// <summary>One-shot body movement a speaker can do while saying a line.</summary>
    public enum GestureType
    {
        None,
        Nod,
        Wave,
        Point,
        Hop,
        Clap,
        ShakeHead,
        Shoo,
        AdjustGlasses,
        Shrug,
        StickTap,
        LookAround
    }

    /// <summary>A single thing a character says.</summary>
    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;

        public Mood mood = Mood.Neutral;

        [Tooltip("Optional body movement that starts with this line.")]
        public GestureType gesture = GestureType.None;

        [Tooltip("Extra seconds to keep the line on screen (only used when lines auto-advance).")]
        [Min(0f)] public float holdSeconds;

        public DialogueLine()
        {
        }

        public DialogueLine(string text, Mood mood = Mood.Neutral, GestureType gesture = GestureType.None)
        {
            this.text = text;
            this.mood = mood;
            this.gesture = gesture;
        }
    }

    /// <summary>A list of lines played in order, as a conversation or a quick comment.</summary>
    [Serializable]
    public class DialogueSequence
    {
        public List<DialogueLine> lines = new List<DialogueLine>();

        [Tooltip("Turn the player's camera to the speaker and pause player control while talking.")]
        public bool focusCamera;

        [Tooltip("Wait for the player to press a key before the next line. Off = lines move on by themselves.")]
        public bool waitForInput;

        public bool IsEmpty
        {
            get
            {
                if (lines == null || lines.Count == 0) return true;
                foreach (DialogueLine line in lines)
                {
                    if (line != null && !string.IsNullOrWhiteSpace(line.text)) return false;
                }
                return true;
            }
        }

        public static DialogueSequence Conversation(params DialogueLine[] lines)
        {
            return new DialogueSequence { lines = new List<DialogueLine>(lines), focusCamera = true, waitForInput = true };
        }

        public static DialogueSequence Comment(params DialogueLine[] lines)
        {
            return new DialogueSequence { lines = new List<DialogueLine>(lines), focusCamera = false, waitForInput = false };
        }
    }
}
