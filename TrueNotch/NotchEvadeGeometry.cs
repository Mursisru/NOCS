using UnityEngine;

namespace NOCS.TrueNotch
{
    internal static class NotchEvadeGeometry
    {
        internal static bool TryComputeEvadeVector(Aircraft aircraft, Vector3 evasionVector, out Vector3 evadeVector)
        {
            evadeVector = Vector3.zero;
            if (aircraft == null || aircraft.rb == null)
                return false;

            Vector3 rhs = Vector3.Cross(evasionVector, aircraft.rb.velocity);
            evadeVector = Vector3.Cross(evasionVector, rhs);
            if (Vector3.Dot(aircraft.transform.forward, evadeVector) < 0f)
                evadeVector *= -1f;

            evadeVector.y = 0f;
            return evadeVector.sqrMagnitude > 0.0001f;
        }

        internal static float ComputeVelocityAngleDeg(Vector3 evadeVector, Vector3 velocity)
        {
            if (evadeVector.sqrMagnitude < 0.0001f || velocity.sqrMagnitude < 0.0001f)
                return 180f;

            return Vector3.Angle(evadeVector, velocity);
        }
    }
}
