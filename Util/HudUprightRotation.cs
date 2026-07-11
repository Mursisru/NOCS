using UnityEngine;

namespace NOCS.Util
{
    internal static class HudUprightRotation
    {
        internal static float ResolveCounterZ(Transform parent)
        {
            if (parent == null)
                return 0f;

            return -Vector3.SignedAngle(parent.up, Vector3.up, Vector3.forward);
        }

        internal static void ApplyLocalUprightZ(RectTransform rect)
        {
            if (rect == null)
                return;

            Transform? parent = rect.parent;
            if (parent == null)
                return;

            float counterZ = ResolveCounterZ(parent);
            Vector3 euler = rect.localEulerAngles;
            rect.localEulerAngles = new Vector3(euler.x, euler.y, counterZ);
        }
    }
}
