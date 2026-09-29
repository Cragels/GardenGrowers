using System;
using UnityEngine;

namespace GardenGrowers.Core
{
    /// <summary>Put on a [SerializeReference] field to get an Inspector dropdown for picking the subclass.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SubclassSelectorAttribute : PropertyAttribute
    {
    }

    /// <summary>Gives a class a nicer name and folder in the subclass dropdown, like "Inventory/Has Item".</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MenuPathAttribute : Attribute
    {
        public string Path { get; }

        public MenuPathAttribute(string path)
        {
            Path = path;
        }
    }
}
