using System;
using System.Collections;

namespace GardenGrowers.Tutorial
{
    /// <summary>Something that happens during a step, like giving an item or fading the screen.</summary>
    [Serializable]
    public abstract class TutorialAction
    {
        public abstract IEnumerator Execute(TutorialContext context);
    }
}
