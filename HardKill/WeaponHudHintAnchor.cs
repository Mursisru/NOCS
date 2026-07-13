using System.Reflection;
using NOCS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.HardKill
{
    /// <summary>
    /// Resolves weapon HUD hint / anchor for ASE status cue. MP-safe: current weaponState may be
    /// guns/no-weapon while APS still needs the missile hint layout.
    /// </summary>
    internal static class WeaponHudHintAnchor
    {
        private static FieldInfo? _combatHudWeaponStateField;
        private static FieldInfo? _hintFieldCache;
        private static System.Type? _hintFieldOwnerType;

        private static Text? _cachedTypographyHint;
        private static int _cachedTypographyFrame = -1;

        internal static bool TryResolve(out Text? typographyHint, out RectTransform? anchor)
        {
            typographyHint = null;
            anchor = null;

            CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
            if (combatHud == null)
                return false;

            if (TryResolveWeaponStateHint(combatHud, out typographyHint, out anchor)
                && anchor != null)
            {
                return true;
            }

            if (TryResolveMissileStateHint(combatHud, out typographyHint, out anchor)
                && anchor != null)
            {
                return true;
            }

            Image? designator = combatHud.targetDesignator;
            if (designator == null)
                return false;

            anchor = designator.rectTransform;
            typographyHint = ResolveTypographyHint(combatHud, null);
            return anchor != null;
        }

        internal static Text? ResolveTypographyHint(CombatHUD combatHud, Text? primary)
        {
            int frame = Time.frameCount;
            if (primary != null)
            {
                _cachedTypographyHint = primary;
                _cachedTypographyFrame = frame;
                return primary;
            }

            if (_cachedTypographyHint != null && _cachedTypographyFrame == frame)
                return _cachedTypographyHint;

            if (_cachedTypographyHint != null)
                return _cachedTypographyHint;

            if (TryResolveMissileStateHint(combatHud, out Text? missileHint, out _)
                && missileHint != null)
            {
                _cachedTypographyHint = missileHint;
                _cachedTypographyFrame = frame;
                return missileHint;
            }

            Text[] texts = combatHud.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text? text = texts[i];
                if (text == null || text.font == null)
                    continue;

                _cachedTypographyHint = text;
                _cachedTypographyFrame = frame;
                return text;
            }

            return null;
        }

        internal static float ResolveInheritedAlpha(RectTransform anchor)
        {
            if (anchor == null)
                return 1f;

            float alpha = 1f;
            Transform? node = anchor;
            while (node != null)
            {
                CanvasGroup? group = node.GetComponent<CanvasGroup>();
                if (group != null)
                    alpha *= Mathf.Clamp01(group.alpha);

                node = node.parent;
            }

            return alpha;
        }

        private static bool TryResolveWeaponStateHint(
            CombatHUD combatHud,
            out Text? hint,
            out RectTransform? anchor)
        {
            hint = null;
            anchor = null;

            HUDWeaponState? weaponState = ResolveCombatHudWeaponState(combatHud);
            if (weaponState == null)
                return false;

            hint = ReadHintField(weaponState);
            if (hint == null)
                return false;

            anchor = hint.rectTransform;
            return anchor != null;
        }

        private static bool TryResolveMissileStateHint(
            CombatHUD combatHud,
            out Text? hint,
            out RectTransform? anchor)
        {
            hint = null;
            anchor = null;

            HUDMissileState? missileState = combatHud.GetComponentInChildren<HUDMissileState>(true);
            if (missileState == null)
                return false;

            hint = ReadHintField(missileState);
            if (hint == null)
                return false;

            anchor = hint.rectTransform;
            return anchor != null;
        }

        private static HUDWeaponState? ResolveCombatHudWeaponState(CombatHUD combatHud)
        {
            _combatHudWeaponStateField ??= typeof(CombatHUD).GetField(
                "weaponState",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return _combatHudWeaponStateField?.GetValue(combatHud) as HUDWeaponState;
        }

        private static Text? ReadHintField(object state)
        {
            if (state == null)
                return null;

            System.Type stateType = state.GetType();
            if (_hintFieldCache == null || _hintFieldOwnerType != stateType)
            {
                _hintFieldOwnerType = stateType;
                _hintFieldCache = stateType.GetField(
                    "hint",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (_hintFieldCache == null)
                return null;

            return _hintFieldCache.GetValue(state) as Text;
        }
    }
}
