using UnityEngine;

namespace RiseOn.Consent.Platforms.IOS {
    internal static class ConsentPlatformBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install() {
            if (Application.platform is not RuntimePlatform.IPhonePlayer) return;

            ConsentPlatform.Install(new ConsentPlatformIOS());
        }
    }
}