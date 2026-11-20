using System.Collections.Generic;
using RiseOn.Utils.Editor;
using UnityEditor;

namespace RiseOn.Consent.Editor.Adapters {
    /// <summary>
    /// Google Mobile Ads keeps its settings in an internal ScriptableObject: it writes its app ids, Consent's, into the manifest and Info.plist, and its tracking text into Info.plist whenever that field has any.
    /// </summary>
    internal sealed class GoogleMobileAdsAdapter : ConsentAdapter {
        private const string AssetPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string Where     = "Assets > Google Mobile Ads > Settings";

        public override string Name => "Google Mobile Ads";

        public override IEnumerable<SettingsIssue> Issues(ConsentSettings settings) {
            var asset = Load(AssetPath);
            foreach (var issue in AppIds(asset, "adMobAndroidAppId", "adMobIOSAppId", settings, Where)) yield return issue;

            var tracking = Field(asset, "userTrackingUsageDescription");
            if (!string.IsNullOrWhiteSpace(tracking?.stringValue)) {
                yield return new SettingsIssue(BuildTarget.iOS, true, $"{Name} has its own tracking prompt; only Consent writes it ({Where}).", "Clear it", Set(asset, "userTrackingUsageDescription", ""));
            }
        }

        public override void Sync(ConsentSettings settings, bool emptyOnly = false) {
            SyncAppIds(Load(AssetPath), "adMobAndroidAppId", "adMobIOSAppId", settings, emptyOnly);
        }
    }
}