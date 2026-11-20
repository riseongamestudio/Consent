using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// The localized resources a target copies into the app, read from project.pbxproj text: each variant group's files and each folder reference in the target's resources phase, plus every file of the variant groups with a given name, which PBXProject.AddLocaleVariantFile reuses and so hands to the app with all their files.<br/>
    /// Unity's PBXProject cannot answer this, since after a project is read back its path lookups find no file of a variant group, its own included.
    /// </summary>
    internal static class XcodeResources {
        public readonly struct Entry {
            /// <summary>The path relative to the Xcode project's folder.</summary>
            public readonly string Path;

            /// <summary>The PBXFileReference's guid.</summary>
            public readonly string Guid;

            /// <summary>A folder reference, copied whole, rather than one file of a variant group.</summary>
            public readonly bool Folder;

            public Entry(string path, string guid, bool folder) {
                Path   = path;
                Guid   = guid;
                Folder = folder;
            }
        }

        // One object of the objects section, written by Xcode or Unity: on one line, or over lines up to its own
        // closing brace at the object's indent.
        private static readonly Regex objectPattern = new(@"^(?<indent>\t+)(?<guid>[0-9A-Fa-f]{24})(?: /\*.*?\*/)? = \{(?<body>[^\n]*\};|\n.*?\n\k<indent>\};)[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex guidPattern = new(@"\b[0-9A-Fa-f]{24}\b");

        public static List<Entry> Copied(string pbxproj, string resourcesPhase, string variantGroupName) {
            var objects = new Dictionary<string, string>();
            foreach (Match match in objectPattern.Matches(pbxproj)) objects[match.Groups["guid"].Value] = match.Groups["body"].Value;

            // A group can sit in two groups (AddLocaleVariantFile adds a reused one to the main group); Xcode then
            // takes the one nearest the main group, measured with Xcode 26.6, so the parents come breadth-first.
            var parents = new Dictionary<string, string>();
            var main    = objects.Values.Where(body => Field(body, "isa") == "PBXProject").Select(body => Field(body, "mainGroup")).FirstOrDefault();
            var queue   = new Queue<string>();
            if (main != null) queue.Enqueue(main);
            while (queue.Count > 0) {
                var group = queue.Dequeue();
                if (!objects.TryGetValue(group, out var body)) continue;
                foreach (var child in List(body, "children")) {
                    if (child == main || parents.ContainsKey(child)) continue;
                    parents[child] = group;
                    queue.Enqueue(child);
                }
            }

            var result  = new List<Entry>();
            var copied  = new List<string>();
            if (objects.TryGetValue(resourcesPhase, out var phase)) {
                foreach (var buildFile in List(phase, "files")) {
                    if (objects.TryGetValue(buildFile, out var buildBody) && Field(buildBody, "fileRef") is { } fileRef) copied.Add(fileRef);
                }
            }

            // A named group the target does not copy yet joins the main group once a file is added to it, which
            // moves where its files resolve; they are read from there.
            foreach (var (guid, body) in objects) {
                if (Field(body, "isa") != "PBXVariantGroup" || Field(body, "name") != variantGroupName || copied.Contains(guid)) continue;
                copied.Add(guid);
                if (main != null) parents[guid] = main;
            }

            foreach (var fileRef in copied.Distinct()) {
                if (!objects.TryGetValue(fileRef, out var reference)) continue;

                switch (Field(reference, "isa")) {
                    case "PBXVariantGroup":
                        foreach (var child in List(reference, "children")) {
                            var path = RealPath(child);
                            if (path != null && result.All(entry => entry.Path != path)) result.Add(new Entry(path, child, false));
                        }

                        break;
                    case "PBXFileReference" when Field(reference, "lastKnownFileType") == "folder": {
                        var path = RealPath(fileRef);
                        if (path != null) result.Add(new Entry(path, fileRef, true));
                        break;
                    }
                }
            }

            return result;

            // Xcode's rule: <group> paths add up through the enclosing groups (a variant group adds nothing),
            // SOURCE_ROOT starts at the project's folder.
            string RealPath(string guid) {
                if (!objects.TryGetValue(guid, out var body)) return null;
                var parts = new List<string>();
                var at    = guid;
                while (true) {
                    var tree = Field(body, "sourceTree");
                    var path = Field(body, "path");
                    if (path != null && Field(body, "isa") != "PBXVariantGroup") parts.Add(path);
                    if (tree == "SOURCE_ROOT") break;
                    if (tree != "<group>") return null;
                    if (!parents.TryGetValue(at, out at) || !objects.TryGetValue(at, out body)) break;
                }

                parts.Reverse();
                return string.Join("/", parts);
            }
        }

        /// <summary>
        /// project.pbxproj text without the given file references: their objects and the lines that list them as a group's children.<br/>
        /// Unity's PBXProject cannot do this for a variant group's file read back from disk.
        /// </summary>
        public static string Remove(string pbxproj, ICollection<string> guids) {
            pbxproj = new Regex(objectPattern + @"\n?", RegexOptions.Multiline | RegexOptions.Singleline).Replace(pbxproj, match => guids.Contains(match.Groups["guid"].Value) ? "" : match.Value);
            foreach (var guid in guids) pbxproj = Regex.Replace(pbxproj, $@"^[ \t]*{guid}(?: /\*[^\n]*?\*/)?,[ \t]*\r?\n", "", RegexOptions.Multiline);
            return pbxproj;
        }

        private static string Field(string body, string name) {
            var match = Regex.Match(body, $@"(?<![\w.]){Regex.Escape(name)} = (""(?<value>(?:[^""\\]|\\.)*)""|(?<value>[^;\s]+))(?:\s*/\*.*?\*/)?;");
            return match.Success ? match.Groups["value"].Value : null;
        }

        private static IEnumerable<string> List(string body, string name) {
            var match = Regex.Match(body, $@"(?<![\w.]){Regex.Escape(name)} = \((?<items>.*?)\);", RegexOptions.Singleline);
            return match.Success ? guidPattern.Matches(Regex.Replace(match.Groups["items"].Value, @"/\*.*?\*/", "")).Select(item => item.Value) : Enumerable.Empty<string>();
        }
    }
}