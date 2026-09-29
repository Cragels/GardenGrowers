using System;
using System.Collections.Generic;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.Tutorial.Conditions
{
    /// <summary>Done when every rule inside is done.</summary>
    [Serializable, MenuPath("Logic/All Of")]
    public class AllOfCondition : TutorialCondition
    {
        [SerializeReference, SubclassSelector] public List<TutorialCondition> conditions = new List<TutorialCondition>();

        public override void Begin(TutorialContext context)
        {
            foreach (TutorialCondition condition in conditions) condition?.Begin(context);
        }

        public override bool IsMet(TutorialContext context)
        {
            foreach (TutorialCondition condition in conditions)
            {
                if (condition != null && !condition.IsMet(context)) return false;
            }
            return true;
        }
    }

    /// <summary>Done when any rule inside is done.</summary>
    [Serializable, MenuPath("Logic/Any Of")]
    public class AnyOfCondition : TutorialCondition
    {
        [SerializeReference, SubclassSelector] public List<TutorialCondition> conditions = new List<TutorialCondition>();

        public override void Begin(TutorialContext context)
        {
            foreach (TutorialCondition condition in conditions) condition?.Begin(context);
        }

        public override bool IsMet(TutorialContext context)
        {
            foreach (TutorialCondition condition in conditions)
            {
                if (condition != null && condition.IsMet(context)) return true;
            }
            return false;
        }
    }

    /// <summary>Flips another rule. For example Not(Weeds Present) = skip if there are no weeds.</summary>
    [Serializable, MenuPath("Logic/Not")]
    public class NotCondition : TutorialCondition
    {
        [SerializeReference, SubclassSelector] public TutorialCondition condition;

        public override void Begin(TutorialContext context) => condition?.Begin(context);

        public override bool IsMet(TutorialContext context) => condition == null || !condition.IsMet(context);
    }
}
