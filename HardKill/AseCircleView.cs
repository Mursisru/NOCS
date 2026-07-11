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
        private const int MaxGlyphs = 12;
        private const float LabelFontRefPx = 12f;
        private const float LabelGapRefPx = 3f;
        private const float GlyphWidthFactor = 0.58f;
        private const float GlyphRadialHalfFactor = 0.32f;
        private const float MaxBankArcSpanDeg = 72f;

        private static readonly float[] LabelAnglesDeg = { 45f, 135f, 225f, 315f };
        private static readonly string TextShoot = "SHOOT";
        private static readonly string TextPossibleHit = "POSSIBLE HIT";
        private static readonly string[] ShootGlyphs = BuildGlyphCache(TextShoot);
        private static readonly string[] PossibleHitGlyphs = BuildGlyphCache(TextPossibleHit);

        private enum CueMode : byte
        {
            Hidden = 0,
            PossibleHit = 1,
            Shoot = 2,
        }

        private readonly GameObject _root;
        private readonly Image _image;
        private readonly RectTransform _rect;
        private readonly GameObject[] _bankRoots = new GameObject[LabelCount];
        private readonly RectTransform[][] _glyphRects = new RectTransform[LabelCount][];
        private readonly TextMeshProUGUI[][] _glyphs = new TextMeshProUGUI[LabelCount][];
        private readonly bool[][] _glyphActive = new bool[LabelCount][];

        private readonly float _fontSizePx;
        private readonly float _gapPx;
        private readonly float _glyphCellPx;

        private float _lastDiameter = -1f;
        private float _lastStroke = -1f;
        private int _lastResolutionStamp = -1;
        private int _lastFillAlpha = -1;
        private CueMode _cueMode = CueMode.Hidden;
        private bool _labelsVisible;
        private int _activeGlyphCount;
        private string[] _activeGlyphSource = PossibleHitGlyphs;
        private Color _lastLabelColor = new Color(0f, 0f, 0f, 0f);

        internal AseCircleView(Transform canvasRoot)
        {
            _fontSizePx = NocsScreenScale.Px(LabelFontRefPx);
            _gapPx = NocsScreenScale.Px(LabelGapRefPx);
            _glyphCellPx = _fontSizePx * GlyphWidthFactor;

            _root = new GameObject("NOCS_AseCircle");
            _root.transform.SetParent(canvasRoot, false);

            _image = _root.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.type = Image.Type.Simple;
            _image.color = Color.green;

            if (AseNotchStyle.TryGetNotchReference(out Image? notchRef) && notchRef != null)
                AseNotchStyle.ApplyImageStyle(_image, notchRef);

            float stroke = AseNotchStyle.ResolveStrokePx();
            _image.sprite = AseCircleSprite.Get(64f, stroke, AseNotchStyle.ResolveRingPixelAlpha());

            _rect = _image.rectTransform;
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.localScale = Vector3.one;
            _rect.sizeDelta = Vector2.zero;

            BuildArcLabels();
            _root.transform.SetAsLastSibling();
            SetVisible(false);
        }

        internal void SetVisible(bool visible)
        {
            if (_image != null)
                _image.enabled = visible;

            if (!visible)
                SetLabelsVisible(false);
        }

        internal void Apply(
            in SwarmInterceptSample sample,
            Aircraft aircraft,
            WeaponStation? defensiveStation)
        {
            if (!sample.Valid || sample.ScreenDiameterPx <= 0f || sample.ThreatCount <= 0)
            {
                SetVisible(false);
                SetCueMode(CueMode.Hidden);
                return;
            }

            if (AseNotchStyle.TryGetNotchReference(out Image? notchRef) && notchRef != null)
                AseNotchStyle.ApplyImageStyle(_image, notchRef);

            float diameter = sample.ScreenDiameterPx;
            float stroke = AseNotchStyle.ResolveStrokePx();
            byte fillAlpha = AseNotchStyle.ResolveRingPixelAlpha();
            int resStamp = AseCircleSprite.SyncResolutionStamp();
            if (!Mathf.Approximately(diameter, _lastDiameter)
                || !Mathf.Approximately(stroke, _lastStroke)
                || resStamp != _lastResolutionStamp
                || fillAlpha != _lastFillAlpha)
            {
                _image.sprite = AseCircleSprite.Get(diameter, stroke, fillAlpha);
                _lastDiameter = diameter;
                _lastStroke = stroke;
                _lastResolutionStamp = AseCircleSprite.ResolutionStamp;
                _lastFillAlpha = fillAlpha;
            }

            Color ringColor = AseNotchStyle.ResolveColor(sample.UrgentThreat);
            _image.color = ringColor;
            _rect.position = new Vector3(sample.ScreenCenter.x, sample.ScreenCenter.y, _rect.position.z);
            _rect.sizeDelta = new Vector2(diameter, diameter);
            _root.transform.SetAsLastSibling();
            SetVisible(true);

            CueMode mode = ResolveCueMode(in sample, aircraft, defensiveStation);
            SetCueMode(mode);
            if (mode == CueMode.Hidden)
                return;

            float radiusPx = diameter * 0.5f;
            if (!ArcLabelsFit(radiusPx, stroke, _activeGlyphCount))
            {
                SetLabelsVisible(false);
                return;
            }

            ApplyLabelColor(AseNotchStyle.ResolveLabelColor(ringColor));
            LayoutArcLabels(radiusPx, stroke);
            SetLabelsVisible(true);
        }

        internal void Dispose()
        {
            if (_root != null)
                Object.Destroy(_root);
        }

        private void BuildArcLabels()
        {
            TMP_FontAsset? font = TMP_Settings.defaultFontAsset;
            float cell = Mathf.Max(8f, _fontSizePx * 1.1f);

            for (int bank = 0; bank < LabelCount; bank++)
            {
                GameObject bankGo = new GameObject("NOCS_AseCueBank_" + bank);
                bankGo.transform.SetParent(_root.transform, false);
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

            _labelsVisible = false;
            _cueMode = CueMode.Hidden;
            _activeGlyphCount = 0;
        }

        private static CueMode ResolveCueMode(
            in SwarmInterceptSample sample,
            Aircraft aircraft,
            WeaponStation? defensiveStation)
        {
            if (!sample.Valid || sample.ThreatCount <= 0)
                return CueMode.Hidden;

            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return CueMode.Hidden;

            bool shoot;
            if (defensiveStation != null)
                shoot = HotTriggerGate.IsLaunchAllowed(in sample, defensiveStation, aircraft);
            else
                shoot = IsGunCrossInsideAll(in sample);

            return shoot ? CueMode.Shoot : CueMode.PossibleHit;
        }

        private static bool IsGunCrossInsideAll(in SwarmInterceptSample sample)
        {
            FlightHud? hud = SceneSingleton<FlightHud>.i;
            if (hud == null || hud.velocityVector == null)
                return false;

            Vector3 pos = hud.velocityVector.transform.position;
            return HotTriggerGate.CircleContains(in sample, new Vector2(pos.x, pos.y));
        }

        private void SetCueMode(CueMode mode)
        {
            if (mode == CueMode.Hidden)
            {
                _cueMode = CueMode.Hidden;
                SetLabelsVisible(false);
                return;
            }

            if (mode != _cueMode)
            {
                _activeGlyphSource = mode == CueMode.Shoot ? ShootGlyphs : PossibleHitGlyphs;
                _activeGlyphCount = _activeGlyphSource.Length;
                ApplyGlyphTexts();
                _cueMode = mode;
                _lastLabelColor.a = -1f;
            }
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

        private void SetLabelsVisible(bool visible)
        {
            if (_labelsVisible == visible)
                return;

            for (int bank = 0; bank < LabelCount; bank++)
            {
                GameObject go = _bankRoots[bank];
                if (go != null)
                    go.SetActive(visible);
            }

            _labelsVisible = visible;
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
