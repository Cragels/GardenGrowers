namespace GardenGrowers.Dialogue
{
    /// <summary>Lets things like animation react while a character talks. DialogueSpeaker calls these.</summary>
    public interface ISpeechReactor
    {
        void OnSpeechStarted();
        void OnLineStarted(DialogueLine line);
        void OnSyllable(char letter, Mood mood);
        void OnLineFinished(DialogueLine line);
        void OnSpeechEnded();
    }

    /// <summary>Something that makes a voice sound for each letter, like BabbleVoice.</summary>
    public interface IDialogueVoice
    {
        /// <summary>True if a sound actually played for this letter.</summary>
        bool Speak(char letter, SpeakerProfile speaker, Mood mood);
    }
}
