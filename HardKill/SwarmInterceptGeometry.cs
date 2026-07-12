using System.Collections.Generic;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal struct ThreatEnvelope
    {
        internal bool Valid;
        internal Vector2 ScreenCenter;
        internal float ScreenRadiusPx;
        internal float ScreenDiameterPx;
        internal float TimeToImpact;
        internal float DistanceMeters;
        internal Missile Threat;
    }

    internal struct SwarmInterceptSample
    {
        internal bool Valid;
        internal int ThreatCount;
        internal Vector2 ScreenCenter;
        internal float ScreenRadiusPx;
        internal float ScreenDiameterPx;
        internal float MinTimeToImpact;
        internal float MinDistanceMeters;
        internal float MaxDistanceMeters;
        internal Missile? UrgentThreat;
    }

    internal static class SwarmInterceptGeometry
    {
        internal const int MaxThreats = 8;
        private const int BoundingRefinePasses = 2;
        private const float BoundingRefineStep = 0.35f;
        private const float MaxMergedRadiusScreenFrac = 0.28f;

        private static readonly ThreatEnvelope[] Envelopes = new ThreatEnvelope[MaxThreats];
        private static int _envelopeCount;

        internal static SwarmInterceptSample Compute(
            Aircraft aircraft,
            IReadOnlyList<Missile> threats,
            WeaponStation? defensiveStation)
        {
            SwarmInterceptSample result = default;
            _envelopeCount = 0;

            if (!NocsGuard.IsValidUnit(aircraft) || threats == null || threats.Count == 0)
                return result;

            float minImpact = float.MaxValue;
            float minDist = float.MaxValue;
            float maxDist = 0f;
            Missile? urgent = null;

            for (int i = 0; i < threats.Count && _envelopeCount < MaxThreats; i++)
            {
                Missile threat = threats[i];
                if (!NocsGuard.IsValidMissile(threat))
                    continue;

                InterceptGeometry.InterceptSample sample = InterceptGeometry.Compute(
                    aircraft,
                    threat,
                    defensiveStation);

                if (!sample.Valid || sample.ScreenDiameterPx <= 0f)
                    continue;

                Envelopes[_envelopeCount] = new ThreatEnvelope
                {
                    Valid = true,
                    ScreenCenter = sample.ScreenCenter,
                    ScreenRadiusPx = sample.ScreenRadiusPx,
                    ScreenDiameterPx = sample.ScreenDiameterPx,
                    TimeToImpact = sample.TimeToImpact,
                    DistanceMeters = sample.DistanceMeters,
                    Threat = threat,
                };

                if (sample.TimeToImpact < minImpact)
                {
                    minImpact = sample.TimeToImpact;
                    urgent = threat;
                }

                if (sample.DistanceMeters < minDist)
                    minDist = sample.DistanceMeters;

                if (sample.DistanceMeters > maxDist)
                    maxDist = sample.DistanceMeters;

                _envelopeCount++;
            }

            if (_envelopeCount <= 0 || urgent == null)
                return result;

            if (_envelopeCount == 1)
            {
                if (!TryBuildSingleThreatCircle(out Vector2 singleCenter, out float singleRadius))
                    return result;

                result.Valid = singleRadius > 0f;
                result.ThreatCount = 1;
                result.ScreenCenter = singleCenter;
                result.ScreenRadiusPx = singleRadius;
                result.ScreenDiameterPx = singleRadius * 2f;
                result.MinTimeToImpact = minImpact;
                result.MinDistanceMeters = minDist;
                result.MaxDistanceMeters = maxDist;
                result.UrgentThreat = urgent;
                return result;
            }

            if (!TryBuildBoundingCircle(out Vector2 mergedCenter, out float mergedRadius))
                return result;

            mergedRadius = ClampMergedRadius(mergedRadius);

            result.Valid = mergedRadius > 0f;
            result.ThreatCount = _envelopeCount;
            result.ScreenCenter = mergedCenter;
            result.ScreenRadiusPx = mergedRadius;
            result.ScreenDiameterPx = mergedRadius * 2f;
            result.MinTimeToImpact = minImpact;
            result.MinDistanceMeters = minDist;
            result.MaxDistanceMeters = maxDist;
            result.UrgentThreat = urgent;
            return result;
        }

        internal static bool TryGetEnvelope(int index, out ThreatEnvelope envelope)
        {
            if (index < 0 || index >= _envelopeCount)
            {
                envelope = default;
                return false;
            }

            envelope = Envelopes[index];
            return envelope.Valid;
        }

        internal static bool GunCrossInsideAllEnvelopes(Vector2 screenPoint)
        {
            return GunCrossInsideAllEnvelopes(screenPoint, 0f);
        }

        internal static bool GunCrossInsideAllEnvelopes(Vector2 screenPoint, float tolerancePx)
        {
            if (_envelopeCount <= 0)
                return false;

            if (float.IsPositiveInfinity(tolerancePx))
                return true;

            float tol = Mathf.Max(0f, tolerancePx);
            for (int i = 0; i < _envelopeCount; i++)
            {
                ThreatEnvelope envelope = Envelopes[i];
                if (!envelope.Valid || envelope.ScreenRadiusPx <= 0f)
                    return false;

                float dx = screenPoint.x - envelope.ScreenCenter.x;
                float dy = screenPoint.y - envelope.ScreenCenter.y;
                float radius = envelope.ScreenRadiusPx + tol;
                if (dx * dx + dy * dy > radius * radius)
                    return false;
            }

            return true;
        }

        internal static bool AllEnvelopesInWeaponRange(WeaponStation? station)
        {
            if (station == null)
                return true;

            for (int i = 0; i < _envelopeCount; i++)
            {
                ThreatEnvelope envelope = Envelopes[i];
                if (!envelope.Valid)
                    continue;

                if (!InterceptGeometry.IsInEnvelope(envelope.DistanceMeters, station))
                    return false;
            }

            return _envelopeCount > 0;
        }

        private static bool TryBuildSingleThreatCircle(out Vector2 center, out float radiusPx)
        {
            center = default;
            radiusPx = 0f;

            if (_envelopeCount != 1)
                return false;

            ThreatEnvelope envelope = Envelopes[0];
            if (!envelope.Valid || envelope.ScreenRadiusPx <= 0f)
                return false;

            center = envelope.ScreenCenter;
            radiusPx = ClampMergedRadius(envelope.ScreenRadiusPx);
            return radiusPx > 0f;
        }

        private static bool TryBuildBoundingCircle(out Vector2 center, out float radiusPx)
        {
            center = default;
            radiusPx = 0f;

            if (_envelopeCount <= 1)
                return false;

            if (!TryResolveNeutralCenter(out center))
                return false;

            for (int pass = 0; pass <= BoundingRefinePasses; pass++)
            {
                radiusPx = ResolveContainRadius(in center);
                if (radiusPx <= 0f)
                    return false;

                if (pass >= BoundingRefinePasses)
                    break;

                if (!TryFindDominantEnvelope(in center, out int dominantIndex))
                    break;

                ThreatEnvelope dominant = Envelopes[dominantIndex];
                center = Vector2.Lerp(center, dominant.ScreenCenter, BoundingRefineStep);
            }

            radiusPx = ClampMergedRadius(ResolveContainRadius(in center));
            return radiusPx > 0f;
        }

        private static float ClampMergedRadius(float radiusPx)
        {
            float maxPx = Screen.width * MaxMergedRadiusScreenFrac;
            return Mathf.Min(Mathf.Max(0f, radiusPx), maxPx);
        }

        private static bool TryResolveNeutralCenter(out Vector2 center)
        {
            float centerX = 0f;
            float centerY = 0f;
            for (int i = 0; i < _envelopeCount; i++)
            {
                centerX += Envelopes[i].ScreenCenter.x;
                centerY += Envelopes[i].ScreenCenter.y;
            }

            center = new Vector2(centerX / _envelopeCount, centerY / _envelopeCount);
            return true;
        }

        private static float ResolveContainRadius(in Vector2 center)
        {
            float containRadius = 0f;
            for (int i = 0; i < _envelopeCount; i++)
            {
                ThreatEnvelope envelope = Envelopes[i];
                float dx = center.x - envelope.ScreenCenter.x;
                float dy = center.y - envelope.ScreenCenter.y;
                float need = Mathf.Sqrt(dx * dx + dy * dy) + envelope.ScreenRadiusPx;
                if (need > containRadius)
                    containRadius = need;
            }

            return containRadius;
        }

        private static bool TryFindDominantEnvelope(in Vector2 center, out int dominantIndex)
        {
            dominantIndex = -1;
            float maxNeed = 0f;

            for (int i = 0; i < _envelopeCount; i++)
            {
                ThreatEnvelope envelope = Envelopes[i];
                float dx = center.x - envelope.ScreenCenter.x;
                float dy = center.y - envelope.ScreenCenter.y;
                float need = Mathf.Sqrt(dx * dx + dy * dy) + envelope.ScreenRadiusPx;
                if (need <= maxNeed)
                    continue;

                maxNeed = need;
                dominantIndex = i;
            }

            return dominantIndex >= 0;
        }
    }
}
