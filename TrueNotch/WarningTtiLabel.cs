using NOCS.Config;
using NOCS.Core;
using NOCS.HardKill;
using NOCS.Util;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.TrueNotch
{
    internal static class WarningTtiLabel
    {
        private const float MaxDisplaySec = 999.9f;
        private const int SuffixCapacity = 16;

        private static readonly StringBuilder Sb = new StringBuilder(96);
        private static readonly char[] TenthsDigits = new char[SuffixCapacity];

        internal static void Apply(ThreatItem item, Missile missile, Aircraft aircraft)
        {
            try
            {
                ApplyCore(item, missile, aircraft);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("WarningTtiLabel.Apply", ex);
            }
        }

        private static void ApplyCore(ThreatItem item, Missile missile, Aircraft aircraft)
        {
            if (!NocsConfigCache.WarningTtiEnabled)
                return;

            if (item == null || !NocsGuard.IsValidMissile(missile) || !NocsGuard.IsLocalPlayerAircraft(aircraft))
                return;

            Text? label = ThreatItemReflection.GetText(item);
            if (label == null)
                return;

            ApplyNotchMatchedColor(item, label);

            float dt = Time.deltaTime;
            if (dt <= 0f)
                dt = 0.016f;

            if (!ThreatImpactTime.TryEstimateSmoothed(missile!, aircraft!, dt, out float ttiSec, out bool impact))
                return;

            if (impact)
            {
                AssignDisplayText(label, ResolveBaseTextLength(label.text), label.text, impact: true, tenths: 0);
            }
            else
            {
                int tenths = Mathf.Clamp(Mathf.RoundToInt(ttiSec * 10f), 0, Mathf.RoundToInt(MaxDisplaySec * 10f));
                AssignDisplayText(label, ResolveBaseTextLength(label.text), label.text, impact: false, tenths);
            }

            HudUprightRotation.ApplyLocalUprightZ(label.rectTransform);
        }

        private static void ApplyNotchMatchedColor(ThreatItem item, Text label)
        {
            Image? notchBox = ThreatItemReflection.GetNotchIndicatorBox(item);
            if (notchBox == null)
                return;

            label.color = AseNotchStyle.ResolveLabelColorFromImage(notchBox);
        }

        private static void AssignDisplayText(Text label, int baseLen, string source, bool impact, int tenths)
        {
            Sb.Clear();
            if (baseLen > 0 && !string.IsNullOrEmpty(source))
                Sb.Append(source, 0, baseLen);

            if (impact)
            {
                Sb.Append(" [IMPACT]");
            }
            else
            {
                Sb.Append(" [");
                AppendTenths(Sb, tenths);
                Sb.Append("s]");
            }

            string current = label.text ?? string.Empty;
            if (current.Length == Sb.Length && TextMatchesBuilder(current))
                return;

            label.text = Sb.ToString();
        }

        private static bool TextMatchesBuilder(string current)
        {
            for (int i = 0; i < Sb.Length; i++)
            {
                if (current[i] != Sb[i])
                    return false;
            }

            return true;
        }

        private static int ResolveBaseTextLength(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int idx = text.LastIndexOf(" [");
            if (idx <= 0)
                return text.Length;

            if (idx + 9 == text.Length && text.EndsWith(" [IMPACT]"))
                return idx;

            if (IsTtiSuffix(text, idx))
                return idx;

            return text.Length;
        }

        private static bool IsTtiSuffix(string text, int suffixStart)
        {
            int len = text.Length - suffixStart;
            if (len < 5)
                return false;

            if (text[suffixStart] != ' ' || text[suffixStart + 1] != '[')
                return false;

            if (text[text.Length - 1] != 's' || text[text.Length - 2] != ']')
                return false;

            int dot = text.LastIndexOf('.', text.Length - 2);
            return dot > suffixStart + 2;
        }

        private static void AppendTenths(StringBuilder sb, int tenths)
        {
            int whole = tenths / 10;
            int frac = tenths % 10;

            if (whole == 0)
            {
                sb.Append('0');
            }
            else
            {
                int len = 0;
                int n = whole;
                while (n > 0 && len < TenthsDigits.Length)
                {
                    TenthsDigits[len++] = (char)('0' + (n % 10));
                    n /= 10;
                }

                for (int i = len - 1; i >= 0; i--)
                    sb.Append(TenthsDigits[i]);
            }

            sb.Append('.');
            sb.Append((char)('0' + frac));
        }
    }
}
