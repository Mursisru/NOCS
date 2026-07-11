using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.TrueNotch
{
    internal static class ThreatItemReflection
    {
        private static FieldInfo? _missileField;
        private static FieldInfo? _notchIndicatorBoxField;
        private static FieldInfo? _textField;

        internal static Missile? GetMissile(ThreatItem item)
        {
            if (item == null)
                return null;

            FieldInfo? field = _missileField ??= typeof(ThreatItem).GetField(
                "missile",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return field?.GetValue(item) as Missile;
        }

        internal static Image? GetNotchIndicatorBox(ThreatItem item)
        {
            if (item == null)
                return null;

            FieldInfo? field = _notchIndicatorBoxField ??= typeof(ThreatItem).GetField(
                "notchIndicatorBox",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return field?.GetValue(item) as Image;
        }

        internal static Text? GetText(ThreatItem item)
        {
            if (item == null)
                return null;

            FieldInfo? field = _textField ??= typeof(ThreatItem).GetField(
                "text",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return field?.GetValue(item) as Text;
        }
    }
}
