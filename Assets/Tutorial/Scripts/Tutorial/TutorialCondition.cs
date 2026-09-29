using System;

namespace GardenGrowers.Tutorial
{
    /// <summary>
    /// A rule that says when a step is done, skipped or failed.
    /// To add one, make a new [Serializable] class that inherits from this.
    /// </summary>
    [Serializable]
    public abstract class TutorialCondition
    {
        /// <summary>Called when the step starts. Remember starting values here (money, day...).</summary>
        public virtual void Begin(TutorialContext context)
        {
        }

        public abstract bool IsMet(TutorialContext context);

        /// <summary>Optional progress text for the HUD, like "1 / 2".</summary>
        public virtual string Progress(TutorialContext context) => null;
    }
}
