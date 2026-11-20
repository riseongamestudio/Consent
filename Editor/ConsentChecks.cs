using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RiseOn.Consent.Editor.Adapters;
using RiseOn.Utils.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// What must hold before a build: the settings the flow and the build steps read, and every known plugin's settings against them.<br/>
    /// The settings page shows the same list with its fixes.
    /// </summary>
    internal sealed class ConsentChecks : IPreprocessBuildWithReport {
        private const string Where = "Project Settings > RiseOn > Consent";

        private static readonly Regex AppIdFormat = new(@"^ca-app-pub-\d+~\d+$");

        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report) {
            var target = report.summary.platform;
            if (target is not BuildTarget.Android and not BuildTarget.iOS) {
                throw new BuildFailedException($"com.riseon.consent runs on Android and iOS only, not {target}; remove the package from builds for {target}.");
            }

            // A plugin's empty copy of the app ids takes Consent's, as a plugin added since has; a differing one is an issue below.
            if (SettingsAssetUtils.FindPath<ConsentSettings>(ConsentSettings.ResourceName) != null) ConsentAdapter.SyncAll(ConsentSettingsProvider.Find(), emptyOnly: true);

            var issues   = Issues().Where(issue => issue.Concerns(target)).ToList();
            var blocking = issues.Where(issue => issue.Blocking).Select(issue => issue.Message).ToList();
            if (blocking.Count > 0) throw new BuildFailedException("Consent: fix these before building, the settings page has a button for most:\n- " + string.Join("\n- ", blocking));

            foreach (var issue in issues) Debug.LogWarning("Consent: " + issue.Message);
        }

        public static IEnumerable<SettingsIssue> Issues() {
            var path = SettingsAssetUtils.FindPath<ConsentSettings>(ConsentSettings.ResourceName);
            if (path == null) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"The project has no Consent settings yet ({Where}).", "Create", () => ConsentSettingsProvider.LoadOrCreate());
                yield break;
            }

            if (!path.Contains("/Resources/")) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{path} is outside a Resources folder, so the game cannot load it and runs with the defaults.", "Move it", () => SettingsAssetUtils.MoveToResources<ConsentSettings>(ConsentSettings.ResourceName));
            }

            var settings = ConsentSettingsProvider.Find();
            foreach (var issue in AppId(settings, settings.AdMobAppIdAndroid, BuildTarget.Android, "adMobAppIdAndroid")) yield return issue;
            foreach (var issue in AppId(settings, settings.AdMobAppIdIOS, BuildTarget.iOS, "adMobAppIdIOS")) yield return issue;

            // Not blocking: a test build uses the sample app on purpose.
            if (settings.AdMobAppIdAndroidTest) yield return new SettingsIssue(BuildTarget.Android, false, SampleApp("Android"), fieldAsset: settings, fieldPath: "adMobAppIdAndroid");
            if (settings.AdMobAppIdIOSTest) yield return new SettingsIssue(BuildTarget.iOS, false, SampleApp("iOS"), fieldAsset: settings, fieldPath: "adMobAppIdIOS");

            if (!settings.ChildDirected) {
                foreach (var problem in TrackingPrompts.Problems(settings)) yield return new SettingsIssue(BuildTarget.iOS, true, problem);
            }

            foreach (var adapter in ConsentAdapter.All) {
                foreach (var issue in adapter.Issues(settings)) yield return issue;
            }
        }

        private static string SampleApp(string platform) => $"Test is on for the {platform} AdMob app id: a build uses Google's sample app, which no consent message or ad unit of the game's belongs to; turn it off before a store build.";

        private static IEnumerable<SettingsIssue> AppId(ConsentSettings settings, string appId, BuildTarget target, string field) {
            if (appId.Length == 0) {
                yield return new SettingsIssue(target, true, $"No AdMob {target} app id; UMP reads its consent form by it.", fieldAsset: settings, fieldPath: field);
            } else if (!AppIdFormat.IsMatch(appId)) {
                yield return new SettingsIssue(target, true, $"\"{appId}\" is not an AdMob app id, which reads ca-app-pub-<publisher>~<app>; an ad unit id has a / instead.", fieldAsset: settings, fieldPath: field);
            }
        }
    }
}