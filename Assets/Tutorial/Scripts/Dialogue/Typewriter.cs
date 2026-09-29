using System;
using TMPro;

namespace GardenGrowers.Dialogue
{
    /// <summary>Reveals TextMeshPro text letter by letter. The SpeechBubble owns one and ticks it.</summary>
    public class Typewriter
    {
        private readonly TMP_Text text;

        private float charactersPerSecond = 30f;
        private float punctuationPause = 0.15f;
        private float timer;
        private int visible;
        private int total;

        public bool IsTyping { get; private set; }

        /// <summary>Raised for every letter that appears. The voice listens to this.</summary>
        public event Action<char> CharacterRevealed;

        /// <summary>Raised once the whole line is visible.</summary>
        public event Action Finished;

        public Typewriter(TMP_Text text)
        {
            this.text = text;
        }

        public void Begin(string content, float charactersPerSecond, float punctuationPause)
        {
            this.charactersPerSecond = Math.Max(1f, charactersPerSecond);
            this.punctuationPause = Math.Max(0f, punctuationPause);

            text.text = content ?? string.Empty;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate(true);
            total = text.textInfo.characterCount;
            visible = 0;
            timer = 0.05f; // tiny beat before the first letter
            IsTyping = total > 0;

            if (!IsTyping) Finished?.Invoke();
        }

        /// <summary>A speedMultiplier above 1 types faster (used while the skip key is held).</summary>
        public void Tick(float deltaTime, float speedMultiplier = 1f)
        {
            if (!IsTyping) return;

            timer -= deltaTime * Math.Max(0.01f, speedMultiplier);
            while (timer <= 0f && IsTyping)
            {
                visible++;
                text.maxVisibleCharacters = visible;

                char letter = text.textInfo.characterInfo[visible - 1].character;
                CharacterRevealed?.Invoke(letter);

                timer += 1f / charactersPerSecond + PauseAfter(letter);

                if (visible >= total) FinishTyping();
            }
        }

        /// <summary>Show the rest of the line immediately.</summary>
        public void Complete()
        {
            if (!IsTyping) return;
            visible = total;
            text.maxVisibleCharacters = total;
            FinishTyping();
        }

        private void FinishTyping()
        {
            IsTyping = false;
            Finished?.Invoke();
        }

        private float PauseAfter(char letter)
        {
            // Only pause at the end of a phrase (next letter is a space or the end).
            bool endOfPhrase = visible >= total || char.IsWhiteSpace(text.textInfo.characterInfo[visible].character);
            if (!endOfPhrase) return 0f;

            switch (letter)
            {
                case '.':
                case '!':
                case '?':
                    return punctuationPause * 2f;
                case ',':
                case ';':
                case ':':
                case '…':
                    return punctuationPause;
                default:
                    return 0f;
            }
        }
    }
}
