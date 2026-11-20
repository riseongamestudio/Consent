using System.Collections.Generic;
using RiseOn.Utils.Editor;

namespace RiseOn.Consent.Editor.Adapters {
    /// <summary>LevelPlay writes its AdMob app ids, Consent's, while its AdMob network is on.</summary>
    internal sealed class LevelPlayAdapter : ConsentAdapter {
        private const string AssetPath = "Assets/LevelPlay/Resources/LevelPlayMediatedNetworkSettings.asset";
        private const string Where     = "Ads Mediation > Developer Settings > Mediated Network Settings";

        public override string Name => "LevelPlay";

        public override IEnumerable<SettingsIssue> Issues(ConsentSettings settings) => AppIds(Load(AssetPath), "AdmobAndroidAppId", "AdmobIOSAppId", settings, Where);

        public override void Sync(ConsentSettings settings, bool emptyOnly = false) => SyncAppIds(Load(AssetPath), "AdmobAndroidAppId", "AdmobIOSAppId", settings, emptyOnly);
    }
}