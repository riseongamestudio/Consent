using System;
using System.Collections.Generic;
using System.IO;
using RiseOn.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Editor.Adapters {
    /// <summary>
    /// AppLovin MAX keeps its AdMob app ids, Consent's, in AppLovinSettings, and its own UMP flow, with its tracking texts, in ProjectSettings/AppLovinInternalSettings.json; that flow must stay off, or the player gets two consent flows.
    /// </summary>
    internal sealed class AppLovinAdapter : ConsentAdapter {
        private const string InternalSettingsPath = "ProjectSettings/AppLovinInternalSettings.json";
        private const string InternalSettingsType = "AppLovinMax.Scripts.IntegrationManager.Editor.AppLovinInternalSettings, MaxSdk.Scripts.IntegrationManager.Editor";
        private const string Where                = "AppLovin > Integration Manager";

        public override string Name => "AppLovin MAX";

        public override IEnumerable<SettingsIssue> Issues(ConsentSettings settings) {
            foreach (var issue in AppIds(Find("AppLovinSettings"), "adMobAndroidAppId", "adMobIosAppId", settings, Where)) yield return issue;

            if (ConsentFlowEnabled()) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{Name} runs its own consent flow, so players would get two ({Where}).", "Turn it off", DisableConsentFlow);
            }
        }

        public override void Sync(ConsentSettings settings, bool emptyOnly = false) => SyncAppIds(Find("AppLovinSettings"), "adMobAndroidAppId", "adMobIosAppId", settings, emptyOnly);

        private static bool ConsentFlowEnabled() {
            if (!File.Exists(InternalSettingsPath)) return false;

            var settings = JsonUtility.FromJson<InternalSettings>(File.ReadAllText(InternalSettingsPath));
            return settings != null && settings.consentFlowEnabled;
        }

        // Through AppLovin's own setter, so the copy it holds in memory does not write the flag back.
        private static void DisableConsentFlow() {
            var type     = Type.GetType(InternalSettingsType, true);
            var instance = type.GetProperty("Instance")!.GetValue(null);
            type.GetProperty("ConsentFlowEnabled")!.SetValue(instance, false);
            type.GetMethod("Save")!.Invoke(instance, null);
        }

        [Serializable]
        private sealed class InternalSettings {
            public bool consentFlowEnabled;
        }
    }
}