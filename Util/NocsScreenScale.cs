using UnityEngine;

namespace NOCS.Util
{
    internal static class NocsScreenScale
    {
        internal const float ReferenceHeight = 1080f;

        private static int _cachedWidth = -1;
        private static int _cachedHeight = -1;
        private static float _heightScale = 1f;

        internal static float HeightScale
        {
            get
            {
                int w = Screen.width;
                int h = Screen.height;
                if (w != _cachedWidth || h != _cachedHeight)
                {
                    _cachedWidth = w;
                    _cachedHeight = h;
                    _heightScale = h > 0 ? Mathf.Max(0.25f, h / ReferenceHeight) : 1f;
                }

                return _heightScale;
            }
        }

        internal static float Px(float referencePixels) => referencePixels * HeightScale;

        /// <summary>
        /// Angular error (radians) → screen pixels using vertical FOV of the main camera.
        /// </summary>
        internal static float RadiansToPx(float radians)
        {
            if (radians <= 0f)
                return 0f;

            float vFovRad = 60f * Mathf.Deg2Rad;
            Camera? cam = SceneSingleton<CameraStateManager>.i?.mainCamera;
            if (cam != null)
                vFovRad = cam.fieldOfView * Mathf.Deg2Rad;

            float pxPerRad = Screen.height / Mathf.Max(0.01f, vFovRad);
            return radians * pxPerRad;
        }

        internal static float PxToRadians(float pixels)
        {
            if (pixels <= 0f)
                return 0f;

            float vFovRad = 60f * Mathf.Deg2Rad;
            Camera? cam = SceneSingleton<CameraStateManager>.i?.mainCamera;
            if (cam != null)
                vFovRad = cam.fieldOfView * Mathf.Deg2Rad;

            float pxPerRad = Screen.height / Mathf.Max(0.01f, vFovRad);
            return pixels / pxPerRad;
        }
    }
}
