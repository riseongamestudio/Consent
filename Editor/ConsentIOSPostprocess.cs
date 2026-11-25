#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// Writes what the consent flow reads from the app: the AdMob app id and the tracking prompt in Info.plist, and the ATT framework.<br/>
    /// It runs after every other build step it can, so its values are the ones the app keeps, and it stops the build when an earlier step left a different value there, since that step's plugin then expects a value the app will not have.
    /// </summary>
    internal sealed class ConsentIOSPostprocess : IPostprocessBuildWithReport {
        private const string AppIdKey    = "GADApplicationIdentifier";
        private const string TrackingKey = "NSUserTrackingUsageDescription";

        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report) {
            if (report.summary.platform is not BuildTarget.iOS) return;

            var buildPath = report.summary.outputPath;
            var settings  = ConsentSettingsProvider.Find();
            var written   = Written.Read(buildPath);
            var tracking  = settings.ChildDirected ? null : settings.TrackingPrompt;
            var conflicts = new List<string>();

            var plistPath = Path.Combine(buildPath, "Info.plist");
            var plist     = new PlistDocument();
            plist.ReadFromFile(plistPath);
            Claim(AppIdKey, settings.AdMobAppIdIOS, written.appId);
            Claim(TrackingKey, tracking, written.tracking);

            if (conflicts.Count > 0) {
                throw new BuildFailedException($"{nameof(ConsentIOSPostprocess)}: other build steps wrote values Consent owns; remove them there, Consent writes them itself:\n- " + string.Join("\n- ", conflicts));
            }

            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project     = new PBXProject();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
            project.WriteToFile(projectPath);
            plist.WriteToFile(plistPath);
            new Written { appId = settings.AdMobAppIdIOS, tracking = tracking }.Write(buildPath);

            // Info.plist: the value Consent keeps there, null for none.
            void Claim(string key, string value, string previous) {
                var existing = plist.root.values.TryGetValue(key, out var element) ? element : null;
                if (existing != null && !(existing is PlistElementString text && (text.value == value || text.value == previous))) {
                    conflicts.Add($"Info.plist {key} is {Describe(existing)}, Consent's is {Quote(value)}");
                }

                if (value == null) {
                    plist.root.values.Remove(key);
                } else {
                    plist.root.SetString(key, value);
                }
            }

            string Quote(string value) => value != null ? $"\"{value}\"" : "none, the game is for children";
        }

        private static string Describe(PlistElement element) => element is PlistElementString text ? $"\"{text.value}\"" : $"a {element.GetType().Name}";

        /// <summary>
        /// The values Consent wrote into this Xcode project the last time, kept beside it: a build that appends to the project finds them still there, and they are Consent's own, not another plugin's.
        /// </summary>
        [Serializable]
        private sealed class Written {
            private const string FileName = "RiseOnConsent.json";

            public string appId;
            public string tracking;

            public static Written Read(string buildPath) {
                var path = Path.Combine(buildPath, FileName);
                return File.Exists(path) ? JsonUtility.FromJson<Written>(File.ReadAllText(path)) : new Written();
            }

            public void Write(string buildPath) {
                File.WriteAllText(Path.Combine(buildPath, FileName), JsonUtility.ToJson(this, true));
            }
        }
    }
}
#endif