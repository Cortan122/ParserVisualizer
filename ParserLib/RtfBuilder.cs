using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParserLib {
    /// <summary>
    /// Подсветка того кода, который сейчас выполняются
    /// </summary>
    internal class RtfBuilder {
        /// <summary>
        /// Массив строк RTF кода
        /// </summary>
        private string[] lines;

        private RtfBuilder(string rtf) {
            lines = rtf.Split('\n'); //.Where(e=>e.StartsWith("\\cf")).ToArray();
        }

        /// <summary>
        /// Выделяет жирным правило, соответствующее данному узлу дерева
        /// </summary>
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

        /// <summary>
        /// Завершить построение грамматики
        /// </summary>
        private string End() {
            return string.Join("\n", lines).Replace("{}", "0");
        }

        /// <summary>
        /// Собирает грамматику путём создания экземпляра класса RtfBuilder и вызова его методов HighlightIdentifier и End
        /// </summary>
        public static string Build(string rtf, IEnumerable<ParserTreeToken> tokens) {
            var builder = new RtfBuilder(rtf);
            foreach (var tok in tokens) {
                builder.HighlightIdentifier(tok);
            }
            return builder.End();
        }

        /// <summary>
        /// Возвращает имена правил, которые присутствуют в грамматике
        /// </summary>
        public static IEnumerable<string> GetNames(string rtf) {
            return rtf.Split('\n')
                .Where(e => e.StartsWith("\\cf"))
                .Select(e => e.Split(new string[] { "\\cf4" }, StringSplitOptions.None)[0].Split(' ')[1]);
        }
    }
}
