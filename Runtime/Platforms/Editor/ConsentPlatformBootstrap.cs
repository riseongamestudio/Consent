using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Platforms.Editor {
    internal static class ConsentPlatformBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install() {
            if (!Application.isEditor) return;

            ConsentPlatform.EditorAsIOS = EditorUserBuildSettings.activeBuildTarget is BuildTarget.iOS;
            ConsentPlatform.Install(new ConsentPlatformEditor());
        }
    }
}