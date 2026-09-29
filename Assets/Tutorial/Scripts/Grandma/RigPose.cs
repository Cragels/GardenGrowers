using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>
    /// A pose for Gogo, stored as offsets from her rest pose so poses can be blended and added.
    /// Distances are in metres, angles in degrees (x = pitch, y = yaw, z = roll).
    /// </summary>
    public struct RigPose
    {
        public Vector3 bodyOffset;
        public Vector3 bodyEuler;
        /// <summary>0 = normal. +0.1 = 10% taller and thinner. -0.1 = squashed.</summary>
        public float squash;

        public Vector3 headOffset;
        public Vector3 headEuler;

        public Vector3 freeHandOffset;
        public Vector3 freeHandEuler;

        public Vector3 stickHandOffset;
        public Vector3 stickHandEuler;

        public Vector3 stickEuler;

        public static RigPose Lerp(in RigPose a, in RigPose b, float t)
        {
            return new RigPose
            {
                bodyOffset = Vector3.LerpUnclamped(a.bodyOffset, b.bodyOffset, t),
                bodyEuler = Vector3.LerpUnclamped(a.bodyEuler, b.bodyEuler, t),
                squash = Mathf.LerpUnclamped(a.squash, b.squash, t),
                headOffset = Vector3.LerpUnclamped(a.headOffset, b.headOffset, t),
                headEuler = Vector3.LerpUnclamped(a.headEuler, b.headEuler, t),
                freeHandOffset = Vector3.LerpUnclamped(a.freeHandOffset, b.freeHandOffset, t),
                freeHandEuler = Vector3.LerpUnclamped(a.freeHandEuler, b.freeHandEuler, t),
                stickHandOffset = Vector3.LerpUnclamped(a.stickHandOffset, b.stickHandOffset, t),
                stickHandEuler = Vector3.LerpUnclamped(a.stickHandEuler, b.stickHandEuler, t),
                stickEuler = Vector3.LerpUnclamped(a.stickEuler, b.stickEuler, t)
            };
        }

        public static RigPose operator +(RigPose a, RigPose b)
        {
            return new RigPose
            {
                bodyOffset = a.bodyOffset + b.bodyOffset,
                bodyEuler = a.bodyEuler + b.bodyEuler,
                squash = a.squash + b.squash,
                headOffset = a.headOffset + b.headOffset,
                headEuler = a.headEuler + b.headEuler,
                freeHandOffset = a.freeHandOffset + b.freeHandOffset,
                freeHandEuler = a.freeHandEuler + b.freeHandEuler,
                stickHandOffset = a.stickHandOffset + b.stickHandOffset,
                stickHandEuler = a.stickHandEuler + b.stickHandEuler,
                stickEuler = a.stickEuler + b.stickEuler
            };
        }

        public static RigPose operator *(RigPose a, float weight)
        {
            return new RigPose
            {
                bodyOffset = a.bodyOffset * weight,
                bodyEuler = a.bodyEuler * weight,
                squash = a.squash * weight,
                headOffset = a.headOffset * weight,
                headEuler = a.headEuler * weight,
                freeHandOffset = a.freeHandOffset * weight,
                freeHandEuler = a.freeHandEuler * weight,
                stickHandOffset = a.stickHandOffset * weight,
                stickHandEuler = a.stickHandEuler * weight,
                stickEuler = a.stickEuler * weight
            };
        }
    }
}
