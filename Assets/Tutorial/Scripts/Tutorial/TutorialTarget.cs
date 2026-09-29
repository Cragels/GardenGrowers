using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenGrowers.Tutorial
{
    /// <summary>
    /// Gives a scene object an id (like "spade" or "stall") so tutorial steps can find it.
    /// Steps are assets, so they can't point at scene objects directly.
    /// </summary>
    [DisallowMultipleComponent]
    public class TutorialTarget : MonoBehaviour
    {
        private static readonly Dictionary<string, TutorialTarget> Registry =
            new Dictionary<string, TutorialTarget>(StringComparer.OrdinalIgnoreCase);

        [SerializeField] private string id = "";
        [Tooltip("How high above this object the marker floats (metres).")]
        [SerializeField] private float markerHeight = 1.2f;
        [Tooltip("How close the player must get for 'reach' goals (metres).")]
        [SerializeField, Min(0.5f)] private float reachRadius = 3.5f;

        public string Id => id;
        public float MarkerHeight => markerHeight;
        public float ReachRadius => reachRadius;

        private void OnEnable()
        {
            if (!string.IsNullOrWhiteSpace(id)) Registry[id] = this;
        }

        private void OnDisable()
        {
            if (!string.IsNullOrWhiteSpace(id) && Registry.TryGetValue(id, out TutorialTarget current) && current == this) Registry.Remove(id);
        }

        /// <summary>Used by the setup tool.</summary>
        public void Configure(string newId, float newMarkerHeight, float newReachRadius)
        {
            id = newId;
            markerHeight = newMarkerHeight;
            reachRadius = newReachRadius;
        }

        public static TutorialTarget Find(string targetId, bool includeInactive = false)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return null;
            if (Registry.TryGetValue(targetId, out TutorialTarget target) && target != null) return target;
            if (!includeInactive) return null;

            foreach (TutorialTarget candidate in FindObjectsByType<TutorialTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (string.Equals(candidate.id, targetId, StringComparison.OrdinalIgnoreCase)) return candidate;
            }
            return null;
        }
    }
}
