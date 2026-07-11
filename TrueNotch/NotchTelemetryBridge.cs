using Mirage.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.TrueNotch
{
    internal struct NotchTelemetrySample
    {
        internal bool Valid;
        internal Vector3 EvasionVector;
        internal PersistentID MissileId;
    }

    internal static class NotchTelemetryBridge
    {
        private static NotchTelemetrySample _latest;
        private static Image? _activeNotchBox;

        internal static void Publish(in NotchTelemetrySample sample, Image? notchBox)
        {
            _latest = sample;
            _activeNotchBox = notchBox;
        }

        internal static bool TryPeek(out NotchTelemetrySample sample, out Image? notchBox)
        {
            sample = _latest;
            notchBox = _activeNotchBox;
            return sample.Valid && notchBox != null;
        }

        internal static void Clear()
        {
            _latest = default;
            _activeNotchBox = null;
        }
    }
}
