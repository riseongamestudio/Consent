using System.Text;
using System.Text.RegularExpressions;

namespace RiseOn.Consent.Editor {
    /// <summary>
    /// One key of an Apple .strings file read and written in place, leaving every other line, comment and the file's encoding as they were, since the file may belong to another plugin.
    /// </summary>
    internal static class StringsFile {
        /// <summary>The value the file gives the key, the last one when it has several, or null.</summary>
        public static string Get(string text, string key) {
            string value = null;
            foreach (Match match in Line(key).Matches(text)) value = Unescape(match.Groups[1].Value);
            return value;
        }

        /// <summary>The text with the key set to the value, or removed when the value is null.</summary>
        public static string Set(string text, string key, string value) {
            var line    = value == null ? "" : $"\"{key}\" = \"{Escape(value)}\";\n";
            var matches = Line(key).Matches(text);
            if (matches.Count == 0) {
                if (value == null) return text;
                return text.Length == 0 || text.EndsWith("\n") ? text + line : text + "\n" + line;
            }

            var result = new StringBuilder(text.Length + line.Length);
            var at     = 0;
            for (var i = 0; i < matches.Count; i++) {
                result.Append(text, at, matches[i].Index - at);
                if (i == matches.Count - 1) result.Append(line);
                at = matches[i].Index + matches[i].Length;
            }

            return result.Append(text, at, text.Length - at).ToString();
        }

        public static string Escape(string value) {
            var result = new StringBuilder(value.Length + 8);
            foreach (var c in value) {
                result.Append(c switch {
                    '"'  => "\\\""
                  , '\\' => "\\\\"
                  , '\n' => "\\n"
                  , '\r' => "\\r"
                  , '\t' => "\\t"
                  , _    => c.ToString()
                });
            }

            return result.ToString();
        }

        public static string Unescape(string value) {
            if (value.IndexOf('\\') < 0) return value;

            var result = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++) {
                var c = value[i];
                if (c != '\\' || i + 1 == value.Length) {
                    result.Append(c);
                    continue;
                }

                var next = value[++i];
                switch (next) {
                    case 'n':
                        result.Append('\n');
                        break;
                    case 'r':
                        result.Append('\r');
                        break;
                    case 't':
                        result.Append('\t');
                        break;
                    case 'U' or 'u' when i + 4 < value.Length && int.TryParse(value.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out var code):
                        result.Append((char)code);
                        i += 4;
                        break;
                    default:
                        result.Append(next);
                        break;
                }
            }

            return result.ToString();
        }

        // A whole `"key" = "value";` line, the key quoted or bare, the value spanning lines when it holds newlines.
        private static Regex Line(string key) => new($"^[ \\t]*(?:\"{Regex.Escape(key)}\"|{Regex.Escape(key)})[ \\t]*=[ \\t]*\"((?:[^\"\\\\]|\\\\.)*)\"[ \\t]*;[^\\S\\n]*\\n?", RegexOptions.Multiline);
    }
}