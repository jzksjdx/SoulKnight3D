using UnityEngine;

namespace SoulKnight3D
{
    internal static class MobilePerformanceBootstrap
    {
        private const int MobileTargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureMobileFramePacing()
        {
#if UNITY_ANDROID || UNITY_IOS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MobileTargetFrameRate;
#endif
        }
    }
}
