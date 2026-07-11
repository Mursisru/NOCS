using UnityEngine;

namespace NOCS.Util
{
    internal static class NocsHudPlacement
    {
        internal static Transform? ResolveFlightHudCanvas()
        {
            FlightHud? flightHud = SceneSingleton<FlightHud>.i;
            if (flightHud == null)
                return null;

            Canvas? canvas = flightHud.GetComponentInChildren<Canvas>(true);
            return canvas != null ? canvas.transform : flightHud.transform;
        }
    }
}
