using System.Collections.Generic;
using UnityEngine;

namespace GardenGrowers.Tutorial
{
    /// <summary>The ordered list of steps that make up a tutorial.</summary>
    [CreateAssetMenu(menuName = "Garden Growers/Tutorial/Tutorial Sequence", fileName = "New Tutorial")]
    public class TutorialSequence : ScriptableObject
    {
        public string title = "Gogo's Garden";
        public List<TutorialStep> steps = new List<TutorialStep>();

        public int IndexOf(TutorialStep step) => step == null ? -1 : steps.IndexOf(step);
    }
}
