using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;
using UnityEditor.Build;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// UMP reads the AdMob app id from the merged manifest.<br/>
    /// This writes Consent's into the unityLibrary manifest after every other build step it can, and stops the build when any manifest of the Gradle project names another id, since the merge would then fail or keep a value the other plugin expects.
    /// </summary>
    internal sealed class ConsentAndroidPostprocess : IPostGenerateGradleAndroidProject {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string AppIdKey         = "com.google.android.gms.ads.APPLICATION_ID";

        // Gradle's own output and caches, which hold copies of the manifests.
        private static readonly string[] SkippedFolders = { "build", ".gradle", ".cxx", "intermediates" };

        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path) {
            var appId     = ConsentSettingsProvider.Find().AdMobAppIdAndroid;
            var root      = Path.GetDirectoryName(Path.GetFullPath(path))!;
            var conflicts = new List<string>();
            foreach (var manifest in Manifests(root)) {
                foreach (var value in AppIds(manifest)) {
                    if (value != appId) conflicts.Add($"{Path.GetRelativePath(root, manifest)} has {AppIdKey} \"{value}\", Consent's is \"{appId}\"");
                }
            }

            if (conflicts.Count > 0) {
                throw new BuildFailedException($"{nameof(ConsentAndroidPostprocess)}: other build steps wrote the AdMob app id Consent owns; make them write the same id or none:\n- " + string.Join("\n- ", conflicts));
            }

            var ownPath     = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var document    = new XmlDocument();
            document.Load(ownPath);
            var application = document.SelectSingleNode("/manifest/application") as XmlElement ?? throw new BuildFailedException($"{nameof(ConsentAndroidPostprocess)}: no <application> in {ownPath}");

            var metaData = MetaData(application).FirstOrDefault();
            if (metaData == null) {
                metaData = document.CreateElement("meta-data");
                application.AppendChild(metaData);
            }

            metaData.SetAttribute("name", AndroidNamespace, AppIdKey);
            metaData.SetAttribute("value", AndroidNamespace, appId);
            document.Save(ownPath);
        }

        private static IEnumerable<string> Manifests(string folder) {
            foreach (var file in Directory.EnumerateFiles(folder, "AndroidManifest.xml")) yield return file;

            foreach (var child in Directory.EnumerateDirectories(folder)) {
                if (SkippedFolders.Contains(Path.GetFileName(child))) continue;
                foreach (var file in Manifests(child)) yield return file;
            }
        }

        private static IEnumerable<string> AppIds(string manifestPath) {
            var document = new XmlDocument();
            document.Load(manifestPath);
            return document.SelectSingleNode("/manifest/application") is XmlElement application ? MetaData(application).Select(element => element.GetAttribute("value", AndroidNamespace)) : Enumerable.Empty<string>();
        }

        private static IEnumerable<XmlElement> MetaData(XmlElement application) =>
            application.ChildNodes.OfType<XmlElement>().Where(element => element.Name == "meta-data" && element.GetAttribute("name", AndroidNamespace) == AppIdKey);
    }
}