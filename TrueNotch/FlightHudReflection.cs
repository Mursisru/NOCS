using System.Reflection;
using UnityEngine;

namespace NOCS.TrueNotch
{
    internal static class FlightHudReflection
    {
        private static FieldInfo? _cockpitTransformField;
        private static FieldInfo? _cockpitRbField;

        internal static bool TryGetCockpit(FlightHud hud, out Transform? cockpitTransform, out Rigidbody? cockpitRb)
        {
            cockpitTransform = null;
            cockpitRb = null;
            if (hud == null)
                return false;

            FieldInfo? tf = _cockpitTransformField ??= typeof(FlightHud).GetField(
                "cockpitTransform",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo? rb = _cockpitRbField ??= typeof(FlightHud).GetField(
                "cockpitRB",
                BindingFlags.Instance | BindingFlags.NonPublic);

            cockpitTransform = tf?.GetValue(hud) as Transform;
            cockpitRb = rb?.GetValue(hud) as Rigidbody;
            return cockpitTransform != null && cockpitRb != null;
        }
    }
}
