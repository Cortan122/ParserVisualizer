using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib {
    public class HistoryEntry : IEnumerable<HistoryToken> {
        private HistoryToken[] treeRanges;
        public string RtfGrammar { get; }
        public int CursorPos { get; }

        private bool isTrimmed = false;
        private Dictionary<HistoryToken, HistoryToken> edges;

        internal HistoryEntry(HistoryToken[] ranges, string rtf) {
            RtfGrammar = rtf;
            treeRanges = ranges;
            CursorPos = treeRanges.Max(e => Math.Max(e.StartPos, e.EndPos - 1)) + 1;
        }

        public void SetSettings(bool trim, bool orientation, bool gravity) {
            isTrimmed = trim;
            CalculateDisplayLevels(orientation ^ gravity);
            if (gravity) InvertDisplayLevels();
            edges = null;
        }

        public Dictionary<HistoryToken, HistoryToken> GetEdges() {
            if (this.edges != null) return this.edges;
            var edges = new Dictionary<HistoryToken, HistoryToken>();
            var stacks = new HistoryToken[CursorPos];
            foreach (var tok in this.OrderBy(e => -e.RecLevel)) {
                var end = tok.EndPos;
                if (end == -1) end = CursorPos;

                for (int i = tok.StartPos; i < end; i++) {
                    if (stacks[i] != null) {
                        edges[stacks[i]] = tok;
                    }
                    stacks[i] = tok;
                }
            }
            this.edges = edges;
            return edges;
        }

        private void InvertDisplayLevels() {
            var maxDisplayLevel = this.Max(e => e.DisplayLevel);
            foreach (var tok in this) {
                tok.DisplayLevel = maxDisplayLevel - tok.DisplayLevel;
            }
        }

        private void CalculateDisplayLevels(bool orientation = false) {
            int[] recLvs = new int[CursorPos];

            IEnumerable<HistoryToken> t = this.OrderBy(e => -e.RecLevel);
            if (orientation) t = t.Reverse();
            foreach (var tok in t) {
                var end = tok.EndPos;
                if (end == -1) end = CursorPos;

                var slice = new ArraySegment<int>(recLvs, tok.StartPos, end - tok.StartPos);
                tok.DisplayLevel = slice.Max();

                for (int i = tok.StartPos; i < end; i++) {
                    recLvs[i] = tok.DisplayLevel + 1;
                }
            }
        }

        public override string ToString() {
            return string.Join(" ", (object[])treeRanges);
        }

        public IEnumerator<HistoryToken> GetEnumerator() {
            var t = treeRanges.Where(e => !e.Name.StartsWith("\""));
            if (isTrimmed) t = t.Where(e => !e.Trimmable);
            return t.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
