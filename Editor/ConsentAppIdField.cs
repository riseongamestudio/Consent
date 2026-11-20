using RiseOn.Consent.Editor.Adapters;
using RiseOn.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// The project's AdMob app ids, kept in Consent's settings alone and drawn wherever a page needs them: Consent's page, and under each ad provider whose SDK writes them.<br/>
    /// An edit on any page is the one edit, and every SDK's settings file takes it at once.
    /// </summary>
    /// <example><code>
    /// ConsentAppIdField.Draw();
    /// </code></example>
    public static class ConsentAppIdField {
        private const string Tooltip = "UMP finds the app's consent messages by it, so it is needed even without AdMob ads: AdMob > Apps > App settings. One value for the project: Consent's page and every ad provider that needs it show this same field, and Google Mobile Ads, LevelPlay and AppLovin take it.";

        private static readonly GUIContent test = new("Test", "Build with Google's sample app id on this platform, not the one written here: for trying Google's test units before the game has an AdMob app.");

        private static SerializedObject serialized;

        /// <summary>Draws the field on a row of its own, under <paramref name="label"/> or "AdMob App ID".</summary>
        public static void Draw(string label = "AdMob App ID") {
            var settings = ConsentSettingsProvider.LoadOrCreate();
            if (serialized == null || serialized.targetObject != settings) serialized = new SerializedObject(settings);

            serialized.Update();
            var android     = serialized.FindProperty("adMobAppIdAndroid");
            var ios         = serialized.FindProperty("adMobAppIdIOS");
            var androidTest = serialized.FindProperty("adMobAppIdAndroidTest");
            var iosTest     = serialized.FindProperty("adMobAppIdIOSTest");
            var a  = android.stringValue;
            var i  = ios.stringValue;
            var at = androidTest.boolValue;
            var it = iosTest.boolValue;
            if (!PlatformFieldUtils.Draw(new GUIContent(label, Tooltip), ref a, ref i, test, ref at, ref it, settings, android.propertyPath, ios.propertyPath)) return;

            android.stringValue   = a;
            ios.stringValue       = i;
            androidTest.boolValue = at;
            iosTest.boolValue     = it;
            if (!serialized.ApplyModifiedProperties()) return;

            DeferredSave.Request(settings);
            ConsentAdapter.SyncAll(settings);
        }

        /// <summary>Fills the empty ids of every SDK's settings file now: for a file an SDK has just made, or a network just turned on.</summary>
        public static void Sync() => ConsentAdapter.SyncAll(ConsentSettingsProvider.LoadOrCreate(), emptyOnly: true);

        /// <summary>Makes <paramref name="appId"/> the project's id for <paramref name="target"/> and writes it into every SDK's settings file.</summary>
        internal static void Set(BuildTarget target, string appId) {
            var settings = ConsentSettingsProvider.LoadOrCreate();
            var asset    = new SerializedObject(settings);
            asset.FindProperty(target is BuildTarget.iOS ? "adMobAppIdIOS" : "adMobAppIdAndroid").stringValue = appId;
            asset.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(settings);
            ConsentAdapter.SyncAll(settings);
        }
    }
}