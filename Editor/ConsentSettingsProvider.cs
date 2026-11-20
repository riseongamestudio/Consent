using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// Project Settings > RiseOn > Consent: what blocks the build first, then the fields in the order a project fills them, testing last.<br/>
    /// The settings asset's Inspector only opens this page.
    /// </summary>
    internal static class ConsentSettingsProvider {
        internal const string PagePath = "Project/RiseOn/Consent";

        private static readonly SettingsTree tree       = new();
        private static readonly SectionRows  promptRows = new();

        // A language the drop-down picked after the page drew, added at the next draw, where Odin records it for undo.
        private static string pickedLanguage;

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
              , keywords = new[] { "consent", "GDPR", "UMP", "AdMob", "ATT", "tracking", "privacy", "child", "language" }
            };
        }

        /// <summary>The whole page for one settings asset.</summary>
        internal static void Draw(ConsentSettings settings) {
            DrawIssues();

            var root          = tree.Begin(settings);
            var childDirected = root.Children["childDirected"];
            childDirected.Draw();
            ConsentAppIdField.Draw();

            // A game for children never asks, so the prompt has nothing to say.
            if (childDirected.ValueEntry.WeakSmartValue is false) DrawPrompts(root);

            // Seldom used, so folded until opened.
            using (var section = Section.Foldout("RiseOn.Consent.Testing", "Testing the form", "In Development Builds, on the test devices only, UMP acts as if they were in this region.", open: false)) {
                if (section.Open) {
                    root.Children["debugGeography"].Draw();
                    root.Children["testDeviceHashedIds"].Draw();
                }
            }

            tree.End();
        }

        private static void DrawIssues() {
            SettingsIssue.Draw(ConsentChecks.Issues());
        }

        // The default text on the bar, Info.plist's, which every player whose language has no row reads, then one row a language. A
        // language Consent translates shows its name and Consent's text until the project writes its own, which the reset button
        // takes back; any other shows a field for Apple's language code.
        private static void DrawPrompts(InspectorProperty root) {
            const string tooltip = "Why the game asks to track. The text on this bar is Info.plist's, for every player whose language has no row; each row is a language the game speaks, which the App Store then lists as one of the game's.";
            var list     = root.Children["trackingTranslations"];
            var resolver = (IOrderedCollectionResolver) list.ChildResolver;
            if (pickedLanguage != null) {
                resolver.QueueAdd(new object[] { new ConsentSettings.TrackingTranslation(pickedLanguage, "") });
                pickedLanguage = null;
            }

            using var section = Section.Foldout("RiseOn.Consent.TrackingPrompt", "Tracking prompt (iOS)", tooltip, buttons: 1, padded: false, barValue: true);
            SectionRows.AddButton(section, 0, "language", Languages(list), code => {
                pickedLanguage = code;
                SessionState.SetBool("RiseOn.Consent.TrackingPrompt", true);
            });
            DrawText(section.BarValue, root.Children["trackingPrompt"], TrackingPrompts.English);
            if (!section.Open) return;

            promptRows.Draw(list.Children.Count, i => DrawPrompt(list.Children[i]), "None", remove: resolver.QueueRemoveAt, removeTooltip: "Remove the language");
        }

        private static void DrawPrompt(InspectorProperty entry) {
            var language = entry.Children["language"];
            var text     = entry.Children["text"];
            var code     = ((string) language.ValueEntry.WeakSmartValue ?? "").Trim();
            var row      = EditorGUILayout.GetControlRect();
            var label    = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            var value    = Rect.MinMaxRect(label.xMax + Section.LabelGap, row.y, row.xMax, row.yMax);

            if (TrackingPrompts.Find(code) is { } known) {
                EditorGUI.LabelField(label, $"{known.name} ({known.code})");
                DrawText(value, text, known.text);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var newCode = EditorGUI.DelayedTextField(label, code).Trim();
            if (EditorGUI.EndChangeCheck()) language.ValueEntry.WeakSmartValue = newCode;

            EditorGUI.BeginChangeCheck();
            var newText = EditorGUI.TextField(value, (string) text.ValueEntry.WeakSmartValue ?? "");
            if (EditorGUI.EndChangeCheck()) text.ValueEntry.WeakSmartValue = newText;
        }

        // A text that shows its default until changed; changed back, or emptied, it is the default again, as the reset inside the field makes it.
        private static void DrawText(Rect field, InspectorProperty text, string fallback) {
            var current = (string) text.ValueEntry.WeakSmartValue ?? "";
            var own     = current.Length > 0;
            EditorGUI.BeginChangeCheck();
            var edited = Section.TextField(field, own ? current : fallback, SdfIconType.ArrowCounterclockwise, "Back to Consent's text", own, out var reset);
            if (EditorGUI.EndChangeCheck()) text.ValueEntry.WeakSmartValue = edited == fallback ? "" : edited;
            if (reset) text.ValueEntry.WeakSmartValue = "";
        }

        // The languages Consent translates that the list has none of, then one for any other code.
        private static List<SearchChoice<string>> Languages(InspectorProperty list) {
            var taken = list.Children.Select(entry => TrackingPrompts.Key(((string) entry.Children["language"].ValueEntry.WeakSmartValue ?? "").Trim())).ToHashSet();
            return TrackingPrompts.Builtin.OrderBy(entry => entry.name)
                                  .Where(entry => !taken.Contains(TrackingPrompts.Key(entry.code)))
                                  .Select(entry => new SearchChoice<string>($"{entry.name} ({entry.code})", entry.code))
                                  .Append(new SearchChoice<string>("Other language", ""))
                                  .ToList();
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