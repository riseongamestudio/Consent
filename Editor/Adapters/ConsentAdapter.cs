using System;
using System.Collections.Generic;
using RiseOn.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Editor.Adapters {
    /// <summary>
    /// What Consent knows about one other plugin that writes the same build values or runs its own consent flow.<br/>
    /// The project's AdMob app ids are Consent's: an edit of them, on any page that shows them, is written into the plugin's settings at once by <see cref="Sync"/>, and an empty copy is filled before the build.<br/>
    /// A copy edited in the plugin's own window shows as an issue that stops the build, with a fix that makes it the project's.<br/>
    /// Consent reads the plugin's settings before the build, so a conflict stops the build however late the plugin writes, names the plugin in the message, and offers the fix.<br/>
    /// Plugins without an adapter are only caught by the checks on the build output.
    /// </summary>
    internal abstract class ConsentAdapter {
        public static readonly ConsentAdapter[] All = { new GoogleMobileAdsAdapter(), new AppLovinAdapter(), new LevelPlayAdapter() };

        public abstract string Name { get; }

        public abstract IEnumerable<SettingsIssue> Issues(ConsentSettings settings);

        /// <summary>Writes Consent's AdMob app ids into the plugin's settings, when it has them; with <paramref name="emptyOnly"/>, into an empty field alone.</summary>
        public abstract void Sync(ConsentSettings settings, bool emptyOnly = false);

        /// <summary>Every adapter's <see cref="Sync"/>.</summary>
        public static void SyncAll(ConsentSettings settings, bool emptyOnly = false) {
            foreach (var adapter in All) adapter.Sync(settings, emptyOnly);
        }

        /// <summary>The asset's serialized field, or null when the asset or the field is missing.</summary>
        protected static SerializedProperty Field(SerializedObject asset, string name) => asset?.FindProperty(name);

        protected static SerializedObject Load(string assetPath) {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return asset ? new SerializedObject(asset) : null;
        }

        protected static SerializedObject Find(string typeName) {
            foreach (var path in AssetFindUtils.Paths("t:" + typeName)) {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset && asset.GetType().Name == typeName) return new SerializedObject(asset);
            }

            return null;
        }

        protected static Action Set(SerializedObject asset, string field, string value) => () => {
            asset.Update();
            asset.FindProperty(field).stringValue = value;
            asset.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(asset.targetObject);
        };

        /// <summary>Writes Consent's app ids into the plugin's two fields, saving the file only when either changes.</summary>
        protected static void SyncAppIds(SerializedObject asset, string android, string ios, ConsentSettings settings, bool emptyOnly) {
            var a = Field(asset, android);
            var i = Field(asset, ios);
            if (a == null || i == null) return;

            var changed = Write(a, settings.AdMobAppIdAndroid) | Write(i, settings.AdMobAppIdIOS);
            if (!changed) return;

            asset.ApplyModifiedPropertiesWithoutUndo();
            DeferredSave.Request(asset.targetObject);

            bool Write(SerializedProperty field, string value) {
                var theirs = field.stringValue?.Trim() ?? "";
                if (theirs == value || emptyOnly && theirs.Length > 0) return false;

                field.stringValue = value;
                return true;
            }
        }

        /// <summary>
        /// The plugin's app id fields against Consent's: one edited in the plugin's own window, so neither empty nor Consent's, stops the build, and its fix makes it the project's, written into every plugin's settings.
        /// </summary>
        protected IEnumerable<SettingsIssue> AppIds(SerializedObject asset, string android, string ios, ConsentSettings settings, string where) {
            // A version of the plugin that renamed its fields would otherwise drop out of the sync without a word.
            if (asset != null && (Field(asset, android) == null || Field(asset, ios) == null)) {
                yield return new SettingsIssue(BuildTarget.NoTarget, true, $"{Name}'s settings have no {android} or {ios} field, so Consent cannot keep their AdMob app ids; this {Name} version needs a newer com.riseon.consent.");
                yield break;
            }

            foreach (var issue in AppId(Field(asset, android), settings.AdMobAppIdAndroid, BuildTarget.Android, where)) yield return issue;
            foreach (var issue in AppId(Field(asset, ios), settings.AdMobAppIdIOS, BuildTarget.iOS, where)) yield return issue;
        }

        private IEnumerable<SettingsIssue> AppId(SerializedProperty field, string consent, BuildTarget target, string where) {
            var theirs = field?.stringValue?.Trim() ?? "";
            if (theirs.Length == 0 || theirs == consent) yield break;

            var platform = target is BuildTarget.Android ? "Android" : "iOS";
            yield return new SettingsIssue(target, true, $"{Name} has AdMob {platform} app id {theirs}, the project's is {Quote(consent)} ({where}).", "Use it", () => ConsentAppIdField.Set(target, theirs));
        }

        private static string Quote(string value) => value.Length == 0 ? "empty" : value;
    }

}