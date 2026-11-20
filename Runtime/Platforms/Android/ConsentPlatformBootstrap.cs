using UnityEngine;

namespace RiseOn.Consent.Platforms.Android {
    internal static class ConsentPlatformBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install() {
            if (Application.platform is not RuntimePlatform.Android) return;

            ConsentPlatform.Install(new ConsentPlatformAndroid());
        }
    }
}