using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParserLib {
    internal class RtfBuilder {
        private string[] lines;

        private RtfBuilder(string rtf) {
            lines = rtf.Split('\n'); //.Where(e=>e.StartsWith("\\cf")).ToArray();
        }

        private void HighlightIdentifier(ParserTreeToken t) {
            if (t.Parent == null) return;
            var lineIndex = Array.FindIndex(lines, e => e.StartsWith("\\cf2 " + t.Parent.Name));
            var line = lines[lineIndex];

            var i = 0;
            var regex = @"\\b\{\}(\\cf[0-9] )" + Regex.Escape(t.Name);
            line = Regex.Replace(line, regex, m => {
                if (i++ == t.Index) return @"\b " + m.Groups[1].Value + t.Name;
                return m.Groups[1].Value + t.Name;
            });
            line = line.Replace("\\b{}", "");

            lines[lineIndex] = line;
        }

        private string End() {
            return string.Join("\n", lines).Replace("{}", "0");
        }

        public static string Build(string rtf, IEnumerable<ParserTreeToken> tokens) {
            var builder = new RtfBuilder(rtf);
            foreach (var tok in tokens) {
                builder.HighlightIdentifier(tok);
            }
            return builder.End();
        }

        public static IEnumerable<string> GetNames(string rtf) {
            return rtf.Split('\n')
                .Where(e => e.StartsWith("\\cf"))
                .Select(e => e.Split(new string[] { "\\cf4" }, StringSplitOptions.None)[0].Split(' ')[1]);
        }
    }
}
