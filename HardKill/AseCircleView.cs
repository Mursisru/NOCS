using System.Reflection;
using NOCS.Config;
using NOCS.Core;
using NOCS.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.HardKill
{
    internal sealed class AseCircleView
    {
        private const int LabelCount = 4;
        private const int MaxGlyphs = 16;
        private const float LabelFontRefPx = 12f;
        private const float LabelGapRefPx = 3f;
        private const float GlyphWidthFactor = 0.58f;
        private const float GlyphRadialHalfFactor = 0.32f;
        private const float MaxBankArcSpanDeg = 72f;
        private const string TextShoot = "SHOOT";
        private const string TextPotentialHit = "POTENTIAL HIT";
        private const float StatusCueGapLocalPx = 18f;

        private static readonly float[] LabelAnglesDeg = { 45f, 135f, 225f, 315f };
        private static readonly string[] ShootGlyphs = BuildGlyphCache(TextShoot);
        private static readonly string[] PotentialHitGlyphs = BuildGlyphCache(TextPotentialHit);

        private enum CueMode : byte
        {
            Hidden = 0,
            PotentialHit = 1,
            Shoot = 2,
        }

        private static FieldInfo? _combatHudWeaponStateField;
        private static FieldInfo? _hintFieldCache;
        private static System.Type? _hintFieldOwnerType;

        private readonly GameObject _ringRoot;
        private readonly Image _ringImage;
        private readonly RectTransform _ringRect;
        private readonly GameObject[] _bankRoots = new GameObject[LabelCount];
        private readonly RectTransform[][] _glyphRects = new RectTransform[LabelCount][];
        private readonly TextMeshProUGUI[][] _glyphs = new TextMeshProUGUI[LabelCount][];
        private readonly bool[][] _glyphActive = new bool[LabelCount][];

        private readonly GameObject _statusRoot;
        private readonly RectTransform _statusRect;
        private readonly Text _statusLabel;

        private readonly float _fontSizePx;
        private readonly float _gapPx;
        private readonly float _glyphCellPx;

        private float _lastDiameter = -1f;
        private float _lastStroke = -1f;
        private int _lastResolutionStamp = -1;
        private int _lastFillAlpha = -1;
        private CueMode _cueMode = CueMode.Hidden;
        private bool _ringVisible;
        private bool _arcLabelsVisible;
        private bool _statusVisible;
        private int _activeGlyphCount;
        private string[] _activeGlyphSource = PotentialHitGlyphs;
        private Color _lastLabelColor = new Color(0f, 0f, 0f, 0f);
        private string _lastStatusText = string.Empty;

        internal AseCircleView(Transform canvasRoot)
        {
            _fontSizePx = NocsScreenScale.Px(LabelFontRefPx);
            _gapPx = NocsScreenScale.Px(LabelGapRefPx);
            _glyphCellPx = _fontSizePx * GlyphWidthFactor;

            _ringRoot = new GameObject("NOCS_AseRing");
            _ringRoot.transform.SetParent(canvasRoot, false);

            _ringImage = _ringRoot.AddComponent<Image>();
            _ringImage.raycastTarget = false;
            _ringImage.type = Image.Type.Simple;
            _ringImage.color = Color.green;

            if (AseNotchStyle.TryGetNotchReference(out Image? notchRef) && notchRef != null)
                AseNotchStyle.ApplyImageStyle(_ringImage, notchRef);

            float stroke = AseNotchStyle.ResolveStrokePx();
            _ringImage.sprite = AseCircleSprite.Get(64f, stroke, AseNotchStyle.ResolveRingPixelAlpha());

            _ringRect = _ringImage.rectTransform;
            _ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            _ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            _ringRect.pivot = new Vector2(0.5f, 0.5f);
            _ringRect.localScale = Vector3.one;
            _ringRect.sizeDelta = Vector2.zero;

            BuildArcLabels();

            _statusRoot = new GameObject("NOCS_AseStatusCue");
            _statusRoot.transform.SetParent(canvasRoot, false);

            _statusRect = _statusRoot.AddComponent<RectTransform>();
            _statusRect.localScale = Vector3.one;

            _statusLabel = _statusRoot.AddComponent<Text>();
            _statusLabel.raycastTarget = false;
            _statusLabel.text = string.Empty;

            HideAll();
        }

        internal void SetVisible(bool visible)
        {
            if (!visible)
                HideAll();
        }

        internal void Apply(
            in SwarmInterceptSample sample,
            Aircraft aircraft,
            WeaponStation? defensiveStation)
        {
            if (!sample.Valid || sample.ThreatCount <= 0 || !NocsGuard.IsLocalPlayerAircraft(aircraft))
            {
                HideAll();
                return;
            }

            CueMode mode = ResolveCueMode(in sample, aircraft, defensiveStation);
            if (NocsConfigCache.RenderAseCircle)
                ApplyRing(in sample, mode);
            else
                SetRingVisible(false);

            if (NocsConfigCache.RenderRadialText)
                ApplyStatusCue(in sample, aircraft, defensiveStation, mode);
            else
                SetStatusVisible(false);
        }

        internal void Dispose()
        {
            if (_ringRoot != null)
                Object.Destroy(_ringRoot);
            if (_statusRoot != null)
                Object.Destroy(_statusRoot);
        }

        private void HideAll()
        {
            SetRingVisible(false);
            SetStatusVisible(false);
            SetCueMode(CueMode.Hidden);
        }

        private void ApplyRing(in SwarmInterceptSample sample, CueMode mode)
        {
            if (sample.ScreenDiameterPx <= 0f)
            {
                SetRingVisible(false);
                return;
            }

            if (AseNotchStyle.TryGetNotchReference(out Image? notchRef) && notchRef != null)
                AseNotchStyle.ApplyImageStyle(_ringImage, notchRef);

            float scale = Mathf.Clamp(NocsConfigCache.AseVisualScale, 0.5f, 2f);
            float diameter = sample.ScreenDiameterPx * scale;
            float stroke = AseNotchStyle.ResolveStrokePx();
            byte fillAlpha = AseNotchStyle.ResolveRingPixelAlpha();
            int resStamp = AseCircleSprite.SyncResolutionStamp();
            if (!Mathf.Approximately(diameter, _lastDiameter)
                || !Mathf.Approximately(stroke, _lastStroke)
                || resStamp != _lastResolutionStamp
                || fillAlpha != _lastFillAlpha)
            {
                _ringImage.sprite = AseCircleSprite.Get(diameter, stroke, fillAlpha);
                _lastDiameter = diameter;
                _lastStroke = stroke;
                _lastResolutionStamp = AseCircleSprite.ResolutionStamp;
                _lastFillAlpha = fillAlpha;
            }

            Color ringColor = AseNotchStyle.ResolveColor(sample.UrgentThreat);
            _ringImage.color = ringColor;
            _ringRect.position = new Vector3(sample.ScreenCenter.x, sample.ScreenCenter.y, _ringRect.position.z);
            _ringRect.sizeDelta = new Vector2(diameter, diameter);
            _ringRoot.transform.SetAsLastSibling();
            SetRingVisible(true);

            if (mode == CueMode.Hidden)
            {
                SetArcLabelsVisible(false);
                return;
            }

            SetCueMode(mode);
            float radiusPx = diameter * 0.5f;
            if (!ArcLabelsFit(radiusPx, stroke, _activeGlyphCount))
            {
                SetArcLabelsVisible(false);
                return;
            }

            ApplyLabelColor(AseNotchStyle.ResolveLabelColor(ringColor));
            LayoutArcLabels(radiusPx, stroke);
            SetArcLabelsVisible(true);
        }

        private void ApplyStatusCue(
            in SwarmInterceptSample sample,
            Aircraft aircraft,
            WeaponStation? defensiveStation,
            CueMode mode)
        {
            if (mode == CueMode.Hidden)
            {
                SetStatusVisible(false);
                return;
            }

            if (!TryResolveWeaponHint(out Text? hint) || hint == null)
            {
                SetStatusVisible(false);
                return;
            }

            SetStatusCueMode(mode);
            ApplyHintTypography(hint);
            ApplyNotchColor(sample.UrgentThreat);
            if (!TryPlaceStatusUnderHint(hint))
            {
                SetStatusVisible(false);
                return;
            }

            _statusRoot.transform.SetAsLastSibling();
            SetStatusVisible(true);
        }

        private void BuildArcLabels()
        {
            TMP_FontAsset? font = TMP_Settings.defaultFontAsset;
            float cell = Mathf.Max(8f, _fontSizePx * 1.1f);

            for (int bank = 0; bank < LabelCount; bank++)
            {
                GameObject bankGo = new GameObject("NOCS_AseCueBank_" + bank);
                bankGo.transform.SetParent(_ringRoot.transform, false);
                RectTransform bankRt = bankGo.AddComponent<RectTransform>();
                bankRt.anchorMin = new Vector2(0.5f, 0.5f);
                bankRt.anchorMax = new Vector2(0.5f, 0.5f);
                bankRt.pivot = new Vector2(0.5f, 0.5f);
                bankRt.anchoredPosition = Vector2.zero;
                bankRt.localEulerAngles = Vector3.zero;
                bankRt.localScale = Vector3.one;
                bankRt.sizeDelta = Vector2.zero;

                _bankRoots[bank] = bankGo;
                _glyphRects[bank] = new RectTransform[MaxGlyphs];
                _glyphs[bank] = new TextMeshProUGUI[MaxGlyphs];
                _glyphActive[bank] = new bool[MaxGlyphs];

                for (int g = 0; g < MaxGlyphs; g++)
                {
                    GameObject glyphGo = new GameObject("g" + g);
                    glyphGo.transform.SetParent(bankGo.transform, false);

                    RectTransform rt = glyphGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.localScale = Vector3.one;
                    rt.localEulerAngles = Vector3.zero;
                    rt.sizeDelta = new Vector2(cell, cell);

                    TextMeshProUGUI tmp = glyphGo.AddComponent<TextMeshProUGUI>();
                    tmp.raycastTarget = false;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    tmp.fontSize = _fontSizePx;
                    tmp.enableAutoSizing = false;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.text = string.Empty;
                    if (font != null)
                        tmp.font = font;

                    _glyphRects[bank][g] = rt;
                    _glyphs[bank][g] = tmp;
                    _glyphActive[bank][g] = false;
                    glyphGo.SetActive(false);
                }

                bankGo.SetActive(false);
            }

            _arcLabelsVisible = false;
            _activeGlyphCount = 0;
        }

        private void SetRingVisible(bool visible)
        {
            if (_ringVisible == visible)
                return;

            _ringVisible = visible;
            if (_ringRoot != null)
                _ringRoot.SetActive(visible);

            if (!visible)
                SetArcLabelsVisible(false);
        }

        private void SetArcLabelsVisible(bool visible)
        {
            if (_arcLabelsVisible == visible)
                return;

            for (int bank = 0; bank < LabelCount; bank++)
            {
                GameObject? go = _bankRoots[bank];
                if (go != null)
                    go.SetActive(visible);
            }

            _arcLabelsVisible = visible;
        }

        private void SetStatusVisible(bool visible)
        {
            if (_statusVisible == visible)
                return;

            _statusVisible = visible;
            if (_statusRoot != null)
                _statusRoot.SetActive(visible);
        }

        private void SetCueMode(CueMode mode)
        {
            if (mode == CueMode.Hidden)
            {
                _cueMode = CueMode.Hidden;
                SetArcLabelsVisible(false);
                return;
            }

            if (mode != _cueMode)
            {
                _activeGlyphSource = mode == CueMode.Shoot ? ShootGlyphs : PotentialHitGlyphs;
                _activeGlyphCount = _activeGlyphSource.Length;
                ApplyGlyphTexts();
                _cueMode = mode;
                _lastLabelColor.a = -1f;
            }
        }

        private void SetStatusCueMode(CueMode mode)
        {
            string text = mode == CueMode.Shoot ? TextShoot : TextPotentialHit;
            if (text == _lastStatusText)
                return;

            _statusLabel.text = text;
            _lastStatusText = text;
        }

        private void ApplyGlyphTexts()
        {
            for (int bank = 0; bank < LabelCount; bank++)
            {
                for (int g = 0; g < MaxGlyphs; g++)
                {
                    bool on = g < _activeGlyphCount;
                    TextMeshProUGUI tmp = _glyphs[bank][g];
                    if (on)
                        tmp.text = _activeGlyphSource[g];

                    if (_glyphActive[bank][g] != on)
                    {
                        tmp.gameObject.SetActive(on);
                        _glyphActive[bank][g] = on;
                    }
                }
            }
        }

        private void ApplyLabelColor(Color color)
        {
            if (ColorsApproximatelyEqual(in color, in _lastLabelColor))
                return;

            for (int bank = 0; bank < LabelCount; bank++)
            {
                for (int g = 0; g < _activeGlyphCount; g++)
                    _glyphs[bank][g].color = color;
            }

            _lastLabelColor = color;
        }

        private void ApplyHintTypography(Text hint)
        {
            _statusLabel.font = hint.font;
            _statusLabel.fontSize = hint.fontSize;
            _statusLabel.fontStyle = hint.fontStyle;
            _statusLabel.alignment = hint.alignment;
            _statusLabel.horizontalOverflow = hint.horizontalOverflow;
            _statusLabel.verticalOverflow = hint.verticalOverflow;
            _statusLabel.lineSpacing = hint.lineSpacing;
            _statusLabel.supportRichText = hint.supportRichText;
            _statusLabel.alignByGeometry = hint.alignByGeometry;
            _statusLabel.resizeTextForBestFit = false;

            if (hint.material != null)
                _statusLabel.material = hint.material;
        }

        private void ApplyNotchColor(Missile? threat)
        {
            if (AseNotchStyle.TryGetNotchReference(out Image? notch) && notch != null)
                _statusLabel.color = AseNotchStyle.ResolveLabelColorFromImage(notch);
            else
                _statusLabel.color = AseNotchStyle.ResolveColor(threat);
        }

        private bool TryPlaceStatusUnderHint(Text hint)
        {
            RectTransform hintRt = hint.rectTransform;
            if (_statusRect.parent != hintRt)
                _statusRect.SetParent(hintRt, false);

            _statusRect.anchorMin = new Vector2(0.5f, 0f);
            _statusRect.anchorMax = new Vector2(0.5f, 0f);
            _statusRect.pivot = new Vector2(0.5f, 1f);
            _statusRect.localRotation = Quaternion.identity;
            _statusRect.localScale = Vector3.one;

            float width = hintRt.rect.width;
            if (width < 1f)
                width = hint.preferredWidth;
            if (width < 1f)
                width = hintRt.sizeDelta.x;

            float height = hintRt.rect.height;
            if (height < 1f)
                height = hint.preferredHeight;
            if (height < 1f)
                height = hint.fontSize;

            _statusRect.sizeDelta = new Vector2(Mathf.Max(width, 1f), Mathf.Max(height, 1f));
            _statusRect.anchoredPosition = new Vector2(0f, -StatusCueGapLocalPx);
            return true;
        }

        private bool ArcLabelsFit(float radiusPx, float strokePx, int glyphCount)
        {
            if (glyphCount <= 0)
                return false;

            if (glyphCount == 1)
                return radiusPx > _fontSizePx;

            float outer = radiusPx
                + strokePx * 0.5f
                + _gapPx
                + _fontSizePx * GlyphRadialHalfFactor;
            if (outer < 1f)
                return false;

            float arcStepDeg = (_glyphCellPx / outer) * Mathf.Rad2Deg;
            float arcSpanDeg = (glyphCount - 1) * arcStepDeg;
            float endMarginDeg = (_fontSizePx * 0.5f / outer) * Mathf.Rad2Deg;
            return arcSpanDeg + endMarginDeg * 2f <= MaxBankArcSpanDeg;
        }

        private void LayoutArcLabels(float radiusPx, float strokePx)
        {
            float outer = radiusPx
                + strokePx * 0.5f
                + _gapPx
                + _fontSizePx * GlyphRadialHalfFactor;
            float arcStepDeg = (_glyphCellPx / Mathf.Max(outer, 1f)) * Mathf.Rad2Deg;

            for (int bank = 0; bank < LabelCount; bank++)
            {
                float centerDeg = LabelAnglesDeg[bank];
                float firstDeg = centerDeg + (_activeGlyphCount - 1) * 0.5f * arcStepDeg;
                bool bottomBank = Mathf.Sin(centerDeg * Mathf.Deg2Rad) < 0f;

                for (int g = 0; g < _activeGlyphCount; g++)
                {
                    int slot = bottomBank ? (_activeGlyphCount - 1 - g) : g;
                    float angleDeg = firstDeg - slot * arcStepDeg;
                    float rad = angleDeg * Mathf.Deg2Rad;
                    RectTransform rt = _glyphRects[bank][g];
                    Vector2 pos = new Vector2(Mathf.Cos(rad) * outer, Mathf.Sin(rad) * outer);
                    rt.anchoredPosition = pos;
                    rt.localRotation = ResolveRadialGlyphRotation(pos, bottomBank);
                    rt.localScale = Vector3.one;
                }
            }
        }

        private static CueMode ResolveCueMode(
            in SwarmInterceptSample sample,
            Aircraft aircraft,
            WeaponStation? defensiveStation)
        {
            if (!sample.Valid || sample.ThreatCount <= 0)
                return CueMode.Hidden;

            if (HotTriggerGate.IsAseShootCueActive(in sample, defensiveStation, aircraft))
                return CueMode.Shoot;

            return CueMode.PotentialHit;
        }

        private static bool TryResolveWeaponHint(out Text? hint)
        {
            hint = null;
            CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
            if (combatHud == null)
                return false;

            _combatHudWeaponStateField ??= typeof(CombatHUD).GetField(
                "weaponState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (_combatHudWeaponStateField == null)
                return false;

            object? stateObj = _combatHudWeaponStateField.GetValue(combatHud);
            if (stateObj is not HUDWeaponState weaponState || weaponState == null)
                return false;

            System.Type stateType = weaponState.GetType();
            if (_hintFieldCache == null || _hintFieldOwnerType != stateType)
            {
                _hintFieldOwnerType = stateType;
                _hintFieldCache = stateType.GetField(
                    "hint",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (_hintFieldCache == null)
                return false;

            hint = _hintFieldCache.GetValue(weaponState) as Text;
            return hint != null;
        }

        private static Quaternion ResolveRadialGlyphRotation(Vector2 anchoredPos, bool bottomBank)
        {
            float lenSq = anchoredPos.sqrMagnitude;
            if (lenSq < 0.0001f)
                return Quaternion.identity;

            Vector2 directionFromCenter = anchoredPos / Mathf.Sqrt(lenSq);
            float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg;
            float z = angle - 90f;
            if (bottomBank)
                z += 180f;

            if (z > 180f)
                z -= 360f;
            else if (z < -180f)
                z += 360f;

            return Quaternion.Euler(0f, 0f, z);
        }

        private static string[] BuildGlyphCache(string text)
        {
            string[] glyphs = new string[text.Length];
            for (int i = 0; i < text.Length; i++)
                glyphs[i] = text[i].ToString();
            return glyphs;
        }

        private static bool ColorsApproximatelyEqual(in Color a, in Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f
                && Mathf.Abs(a.g - b.g) < 0.002f
                && Mathf.Abs(a.b - b.b) < 0.002f
                && Mathf.Abs(a.a - b.a) < 0.002f;
        }
    }
}
