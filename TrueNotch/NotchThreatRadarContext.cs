using System.Reflection;
using UnityEngine;

namespace NOCS.TrueNotch
{
    internal struct NotchThreatRadarContext
    {
        internal bool Valid;
        internal RadarParams RadarParams;
        internal Vector3 LosDirection;
        internal float Distance;
        internal float Clutter;
        internal float Rcs;
        internal float PreviewEcm;
        internal float MinSignal;
    }

    internal static class NotchThreatRadarResolver
    {
        private static FieldInfo? _arhRadarParams;
        private static FieldInfo? _sarhRadarParams;
        private static FieldInfo? _sarhRadarSourcePoint;
        private static Aircraft? _jammerCacheAircraft;
        private static float _jammerCacheArh;
        private static float _jammerCacheSarh;

        internal static bool TryResolve(Aircraft aircraft, Missile missile, out NotchThreatRadarContext ctx)
        {
            ctx = default;
            if (aircraft == null || missile == null || aircraft.rb == null)
                return false;

            MissileSeeker? seeker = missile.GetComponent<MissileSeeker>();
            if (seeker == null)
                return false;

            string seekerType = seeker.GetSeekerType();
            Vector3 source;
            RadarParams radarParams;

            switch (seekerType)
            {
                case "ARH":
                    if (!TryGetArhParams(seeker, out radarParams))
                        return false;
                    source = missile.transform.position;
                    break;
                case "SARH":
                    if (!TryGetSarhParams(seeker, out radarParams, out source))
                        return false;
                    break;
                default:
                    return false;
            }

            float dist = Vector3.Distance(source, aircraft.transform.position);
            if (dist <= 1f)
                return false;

            float radarDist = Mathf.Min(dist, radarParams.maxRange);

            Vector3 los = FastMath.NormalizedDirection(source, aircraft.transform.position);
            if (los.sqrMagnitude < 0.0001f)
                return false;

            if (!TryComputeClutter(source, aircraft, radarDist, out float clutter))
                clutter = 0f;

            float jammerMax = ResolveMaxJammerIntensity(aircraft, seekerType);
            float currentEcm = aircraft.GetECMIntensity();

            ctx.Valid = true;
            ctx.RadarParams = radarParams;
            ctx.LosDirection = los;
            ctx.Distance = radarDist;
            ctx.Clutter = clutter;
            ctx.Rcs = aircraft.RCS;
            ctx.PreviewEcm = Mathf.Max(currentEcm, jammerMax);
            ctx.MinSignal = radarParams.minSignal;
            return true;
        }

        private static bool TryGetArhParams(MissileSeeker seeker, out RadarParams radarParams)
        {
            radarParams = default!;
            if (seeker is not ARHSeeker arh)
                return false;

            FieldInfo? field = _arhRadarParams ??= typeof(ARHSeeker).GetField(
                "radarParameters",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(arh) is not RadarParams parameters)
                return false;

            radarParams = parameters;
            return true;
        }

        private static bool TryGetSarhParams(MissileSeeker seeker, out RadarParams radarParams, out Vector3 source)
        {
            radarParams = default!;
            source = Vector3.zero;
            if (seeker is not SARHSeeker sarh)
                return false;

            FieldInfo? paramsField = _sarhRadarParams ??= typeof(SARHSeeker).GetField(
                "radarParams",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo? sourceField = _sarhRadarSourcePoint ??= typeof(SARHSeeker).GetField(
                "radarSourcePoint",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (paramsField?.GetValue(sarh) is not RadarParams parameters)
                return false;

            radarParams = parameters;
            if (sourceField?.GetValue(sarh) is Transform sourceTransform && sourceTransform != null)
                source = sourceTransform.position;
            else
                source = sarh.GetEvasionPoint().ToLocalPosition();

            return true;
        }

        private static bool TryComputeClutter(Vector3 source, Aircraft aircraft, float dist, out float clutter)
        {
            clutter = 0f;
            Vector3 acLocal = aircraft.transform.position;
            Vector3 flatDelta = acLocal - source;
            flatDelta.y = 0f;
            float flatDist = flatDelta.magnitude;

            float sourceY = Mathf.Max(source.y, 0.1f);
            float acY = Mathf.Max(acLocal.y, 0.1f);
            float horizonSource = Mathf.Sqrt(12742000f * sourceY);
            float horizonAc = Mathf.Sqrt(12742000f * acY);
            if (horizonSource + horizonAc < dist)
            {
                float altOnly = Mathf.Max(aircraft.radarAlt, 1f);
                clutter = aircraft.maxRadius * aircraft.maxRadius * 2f / (altOnly * altOnly);
                return true;
            }

            if (flatDist < horizonSource && acY < sourceY * (1f - flatDist / horizonSource))
            {
                float denom = sourceY - acY;
                if (Mathf.Abs(denom) > 0.001f)
                {
                    float rangeFactor = dist * aircraft.radarAlt / denom;
                    clutter += Mathf.Min(dist, 1000f) / rangeFactor;
                }
            }

            float alt = Mathf.Max(aircraft.radarAlt, 1f);
            clutter += aircraft.maxRadius * aircraft.maxRadius * 2f / (alt * alt);
            return true;
        }

        private static float ResolveMaxJammerIntensity(Aircraft aircraft, string seekerType)
        {
            if (_jammerCacheAircraft != aircraft)
            {
                _jammerCacheAircraft = aircraft;
                _jammerCacheArh = 0f;
                _jammerCacheSarh = 0f;
                RadarJammer[] jammers = aircraft.GetComponentsInChildren<RadarJammer>(true);
                for (int i = 0; i < jammers.Length; i++)
                {
                    RadarJammer jammer = jammers[i];
                    if (jammer == null)
                        continue;

                    float intensity = jammer.GetMaxJammingIntensity();
                    var types = jammer.GetThreatTypes();
                    if (types.Contains("ARH"))
                        _jammerCacheArh = Mathf.Max(_jammerCacheArh, intensity);
                    if (types.Contains("SARH"))
                        _jammerCacheSarh = Mathf.Max(_jammerCacheSarh, intensity);
                }
            }

            return seekerType == "SARH" ? _jammerCacheSarh : _jammerCacheArh;
        }
    }
}
