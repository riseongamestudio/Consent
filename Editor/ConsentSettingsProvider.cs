using RiseOn.Utils.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// Project Settings > RiseOn > Consent: what blocks the build first, then the fields in the order a project fills them, testing last.<br/>
    /// The settings asset's Inspector only opens this page.
    /// </summary>
    internal static class ConsentSettingsProvider {
        internal const string PagePath = "Project/RiseOn/Consent";

        private static readonly SettingsTree tree = new();

        private static readonly GUIContent privacyPolicy = new("Privacy policy URL", "The page the game's privacy button opens (ConsentFlow.OpenPrivacyPolicy) and the privacy notice links; RiseOn's until changed.");

        // EditorPrefs keeps the kind plus one, 0 for none.
        private static readonly (string Name, int Value)[] failures = {
            ("None", 0), (nameof(ConsentErrorKind.Network), (int) ConsentErrorKind.Network + 1), (nameof(ConsentErrorKind.Misconfiguration), (int) ConsentErrorKind.Misconfiguration + 1),
            (nameof(ConsentErrorKind.Internal), (int) ConsentErrorKind.Internal + 1)
        };

        // A field's margins above and below for the square beside the privacy policy: a group takes the least of its members', so a
        // square without them would leave the row none.
        private static GUIStyle buttonCell;

        [MenuItem("RiseOn/Consent")]
        internal static void Open() {
            SettingsService.OpenProjectSettings(PagePath);
        }

        [SettingsProvider]
        private static SettingsProvider Create() {
            return new(PagePath, SettingsScope.Project) {
                guiHandler = _ => {
                    using var page = SettingsPage.Begin();
                    var settings = SettingsAssetUtils.Find<ConsentSettings>(ConsentSettings.ResourceName);
                    if (!settings) {
                        DrawIssues();
                        return;
                    }

                    Draw(settings);
                }
              , deactivateHandler = tree.Dispose
              , keywords = new[] { "consent", "GDPR", "UMP", "AdMob", "ATT", "tracking", "privacy", "policy", "notice", "child" }
            };
        }

        /// <summary>The whole page for one settings asset.</summary>
        internal static void Draw(ConsentSettings settings) {
            DrawIssues();

            var root = tree.Begin(settings);
            ConsentAppIdField.Draw();
            DrawPrivacyPolicy(settings, root);
            var childDirected = root.Children["childDirected"];
            childDirected.Draw();

            // A game for children never asks, so the prompt has nothing to say.
            if (childDirected.ValueEntry.WeakSmartValue is false) {
                var (label, value) = Row();
                EditorGUI.LabelField(label, new GUIContent("Tracking prompt (iOS)", "Why the game asks to track, under the title of iOS's prompt; the build writes it into Info.plist."));
                DrawText(value, root.Children["trackingPrompt"], ConsentSettings.DefaultTrackingPrompt, "Back to Consent's text");
            }

            // Seldom used, so folded until opened.
            using (var section = Section.Foldout("RiseOn.Consent.Testing", "Testing the form", "In Development Builds, on the test devices only, UMP acts as if they were in this region. In the Editor, Consent's stand-in dialogs act for it, Disabled being outside every regulated region.", open: false)) {
                if (section.Open) {
                    root.Children["debugGeography"].Draw();
                    DrawEditorTesting();
                    root.Children["testDeviceHashedIds"].Draw();
                }
            }

            tree.End();
        }

        private static void DrawIssues() {
            SettingsIssue.Draw(ConsentChecks.Issues());
        }

        // One page for both platforms, with a button at the row's end to split it; split, the Android and iOS pair, whose button links
        // them back into Android's page, as the Ads page's values split.
        private static void DrawPrivacyPolicy(ConsentSettings settings, InspectorProperty root) {
            var shared = root.Children["privacyPolicyUrl"];
            var split  = root.Children["privacyPolicyUrlSplit"];
            var ios    = root.Children["privacyPolicyUrlIOS"];
            var isSplit = (bool) split.ValueEntry.WeakSmartValue;
            buttonCell ??= new GUIStyle { margin = new RectOffset(0, 0, EditorStyles.textField.margin.top, EditorStyles.textField.margin.bottom) };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            var a = (string) shared.ValueEntry.WeakSmartValue ?? "";
            if (isSplit) {
                var i   = (string) ios.ValueEntry.WeakSmartValue ?? "";
                var off = false;
                if (PlatformFieldUtils.Draw(privacyPolicy, ref a, ref i, null, ref off, ref off, settings, shared.Path, ios.Path)) {
                    shared.ValueEntry.WeakSmartValue = a;
                    ios.ValueEntry.WeakSmartValue    = i;
                }
            } else {
                var field = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(true), privacyPolicy);
                EditorGUI.BeginChangeCheck();
                a = EditorGUI.TextField(field, a);
                if (EditorGUI.EndChangeCheck()) shared.ValueEntry.WeakSmartValue = a;
                SettingsFocus.Drawn(settings, shared.Path, field);
            }

            EditorGUILayout.EndVertical();
            var button = GUILayoutUtility.GetRect(Section.ButtonWidth, EditorGUIUtility.singleLineHeight, buttonCell, GUILayout.Width(Section.ButtonWidth), GUILayout.ExpandWidth(false));
            if (isSplit) {
                if (Section.IconButton(button, SdfIconType.Link, "One page for both platforms again: the Android URL stays, the iOS URL is dropped")) split.ValueEntry.WeakSmartValue = false;
            } else if (Section.IconButton(button, SdfIconType.ViewStacked, "A page for each platform: an Android URL and an iOS URL, both starting as this one")) {
                ios.ValueEntry.WeakSmartValue   = a;
                split.ValueEntry.WeakSmartValue = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        // What the Editor's stand-in does, per person, and a way back to a first launch.
        private static void DrawEditorTesting() {
            var (label, value) = Row();
            EditorGUI.LabelField(label, new GUIContent("Editor failure", "Makes the Editor's stand-in fail the request with this kind of error, to try the game's handling of a failed flow. Kept for you only, not in the project."));
            // The pick lands after this draw, from the drop-down's window, so the page repaints to show it.
            var window = GUIHelper.CurrentWindow;
            ChoiceFieldUtils.Value(value, failures, EditorPrefs.GetInt(ConsentPlatform.EditorFailureKey), picked => {
                EditorPrefs.SetInt(ConsentPlatform.EditorFailureKey, picked);
                if (window) window.Repaint();
            }, search: false);

            (label, value) = Row();
            EditorGUI.LabelField(label, new GUIContent("Editor choices", "The Editor keeps the choices made in its stand-in dialogs across Play sessions, as a device does; reset them to play a first launch again."));
            if (GUI.Button(value, "Reset")) {
                foreach (var key in ConsentPlatform.EditorChoiceKeys) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.DeleteKey(ConsentFlow.NoticeShownKey);
                PlayerPrefs.Save();
            }
        }

        private static (Rect label, Rect value) Row() {
            var row   = EditorGUILayout.GetControlRect();
            var label = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            return (label, Rect.MinMaxRect(label.xMax + Section.LabelGap, row.y, row.xMax, row.yMax));
        }

        // A text that shows its default until changed; changed back, or emptied, it is the default again, as the reset inside the field makes it.
        private static void DrawText(Rect field, InspectorProperty text, string fallback, string resetTooltip) {
            var current = (string) text.ValueEntry.WeakSmartValue ?? "";
            var own     = current.Length > 0;
            EditorGUI.BeginChangeCheck();
            var edited = Section.TextField(field, own ? current : fallback, SdfIconType.ArrowCounterclockwise, resetTooltip, own, out var reset);
            if (EditorGUI.EndChangeCheck()) text.ValueEntry.WeakSmartValue = edited == fallback ? "" : edited;
            if (reset) text.ValueEntry.WeakSmartValue = "";
        }

        /// <summary>The project's settings asset, made with the defaults when it has none.</summary>
        internal static ConsentSettings LoadOrCreate() {
            return SettingsAssetUtils.LoadOrCreate<ConsentSettings>(ConsentSettings.ResourceName);
        }

        /// <summary>For the build steps: the asset wherever the project keeps it, or the defaults.</summary>
        internal static ConsentSettings Find() {
            var settings = SettingsAssetUtils.Find<ConsentSettings>(ConsentSettings.ResourceName);
            return settings ? settings : ScriptableObject.CreateInstance<ConsentSettings>();
        }
    }

    [CustomEditor(typeof(ConsentSettings))]
    internal sealed class ConsentSettingsInspector : SettingsAssetInspector {
        protected override string PagePath => ConsentSettingsProvider.PagePath;
    }
}