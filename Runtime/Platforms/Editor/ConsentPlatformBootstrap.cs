using UnityEngine;

namespace RiseOn.Consent.Platforms.Editor {
    internal static class ConsentPlatformBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install() {
            if (!Application.isEditor) return;

            ConsentPlatform.Install(new ConsentPlatformEditor());
        }
    }
}