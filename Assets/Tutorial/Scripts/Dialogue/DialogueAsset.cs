using UnityEngine;

namespace GardenGrowers.Dialogue
{
    /// <summary>A conversation saved as an asset. Play it with DialogueSpeaker.Say(asset.sequence).</summary>
    [CreateAssetMenu(menuName = "Garden Growers/Dialogue/Dialogue Asset", fileName = "New Dialogue")]
    public class DialogueAsset : ScriptableObject
    {
        public SpeakerProfile speaker;
        public DialogueSequence sequence = new DialogueSequence();
    }
}
