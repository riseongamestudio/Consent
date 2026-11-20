using System;
using System.Collections.Generic;

namespace RiseOn.Consent {
    /// <summary>
    /// Reads, from the IAB GPP string a CMP stores, whether the player opted out of the sale or sharing of their data or of targeted advertising under a US privacy law.<br/>
    /// Only the bits for that are decoded.
    /// </summary>
    internal static class GppUSOptOut {
        private const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private const int    OptedOut  = 1;

        // Section id -> bit offsets of SaleOptOut, SharingOptOut and TargetedAdvertisingOptOut in the core segment,
        // read off the IAB's own encoder (@iabgpp/cmpapi 3.2.0); -1 where a section has no such field.
        private static readonly Dictionary<int, int[]> optOutBits = new() {
            { 7, new[] { 18, 20, 22 } } // usnat
          , { 8, new[] { 12, 14, -1 } } // usca
          , { 9, new[] { 12, -1, 14 } } // usva
          , { 10, new[] { 12, -1, 14 } } // usco
          , { 11, new[] { 14, -1, 16 } } // usut
          , { 12, new[] { 12, -1, 14 } } // usct
          , { 13, new[] { 12, -1, 14 } } // usfl
          , { 14, new[] { 12, -1, 14 } } // usmt
          , { 15, new[] { 12, -1, 14 } } // usor
          , { 16, new[] { 12, -1, 14 } } // ustx
          , { 17, new[] { 12, -1, 14 } } // usde
          , { 18, new[] { 14, -1, 16 } } // usia
          , { 19, new[] { 12, -1, 14 } } // usne
          , { 20, new[] { 12, -1, 14 } } // usnh
          , { 21, new[] { 12, -1, 14 } } // usnj
          , { 22, new[] { 12, -1, 14 } } // ustn
          , { 23, new[] { 12, -1, 14 } } // usmn
          , { 24, new[] { 16, -1, 18 } } // usmd
          , { 25, new[] { 16, -1, 18 } } // usin
          , { 26, new[] { 16, -1, 18 } } // usky
          , { 27, new[] { 16, -1, 18 } } // usri
        };

        /// <param name="gppString">IABGPP_HDR_GppString.</param>
        /// <param name="applicableSectionIds">IABGPP_GppSID: the ids in force, joined by '_'; empty to read every section.</param>
        public static bool Read(string gppString, string applicableSectionIds) {
            if (string.IsNullOrEmpty(gppString)) return false;

            var parts = gppString.Split('~');
            var ids   = HeaderSectionIds(parts[0]);
            var only  = ParseIds(applicableSectionIds);

            for (var i = 0; i < ids.Count && i + 1 < parts.Length; i++) {
                if (only.Count > 0 && !only.Contains(ids[i])) continue;
                if (!optOutBits.TryGetValue(ids[i], out var offsets)) continue;

                var core = Bits(parts[i + 1].Split('.')[0]);
                foreach (var offset in offsets) {
                    if (offset >= 0 && offset + 2 <= core.Length && Int(core, offset, 2) == OptedOut) return true;
                }
            }

            return false;
        }

        // Header core segment: type (6 bits), version (6), then the section ids as a Fibonacci-coded range.
        private static List<int> HeaderSectionIds(string header) {
            var bits  = Bits(header.Split('.')[0]);
            var ids   = new List<int>();
            var count = Int(bits, 12, 12);
            var at    = 24;
            var last  = 0;
            for (var entry = 0; entry < count; entry++) {
                var isRange = bits[at++] == '1';
                var start   = last + Fibonacci(bits, ref at);
                last = start;
                if (!isRange) {
                    ids.Add(start);
                    continue;
                }

                var end = last + Fibonacci(bits, ref at);
                last = end;
                for (var id = start; id <= end; id++) ids.Add(id);
            }

            return ids;
        }

        // Fibonacci coding: bit j adds the j-th of 1, 2, 3, 5, 8…; no two value bits are adjacent, so the first "11"
        // ends the code and its second 1 only terminates.
        private static int Fibonacci(string bits, ref int at) {
            int value = 0, previous = 0, current = 0;
            for (var j = 0; at + j < bits.Length; j++) {
                var fibonacci = j == 0 ? 1 : j == 1 ? 2 : previous + current;
                previous = current;
                current  = fibonacci;
                if (bits[at + j] != '1') continue;

                if (j > 0 && bits[at + j - 1] == '1') {
                    at += j + 1;
                    return value;
                }

                value += fibonacci;
            }

            throw new FormatException("GPP: unterminated Fibonacci integer");
        }

        private static string Bits(string base64Url) {
            var bits = new char[base64Url.Length * 6];
            for (var i = 0; i < base64Url.Length; i++) {
                var value = Base64Url.IndexOf(base64Url[i]);
                if (value < 0) throw new FormatException($"GPP: '{base64Url[i]}' is not base64url");

                for (var b = 0; b < 6; b++) bits[i * 6 + b] = (value >> (5 - b) & 1) == 1 ? '1' : '0';
            }

            return new string(bits);
        }

        private static int Int(string bits, int start, int length) {
            var value = 0;
            for (var i = start; i < start + length; i++) value = value << 1 | (bits[i] == '1' ? 1 : 0);
            return value;
        }

        private static HashSet<int> ParseIds(string ids) {
            var set = new HashSet<int>();
            if (string.IsNullOrEmpty(ids)) return set;

            foreach (var id in ids.Split('_')) {
                if (int.TryParse(id, out var value)) set.Add(value);
            }

            return set;
        }
    }
}