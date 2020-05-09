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
        public HistoryToken[] TreeRanges { get; } // can be private
        public string RtfGrammar { get; }
        public int CursorPos { get; }

        internal HistoryEntry(HistoryToken[] ranges, string rtf) {
            RtfGrammar = rtf;
            TreeRanges = ranges;
            CursorPos = TreeRanges.Max(e => Math.Max(e.StartPos, e.EndPos - 1)) + 1;
            CalculateDisplayLevels();
        }

        private void AssignDisplayLevels() {
            var maxRecLevel = this.Max(e => e.RecLevel);
            foreach (var tok in this) {
                tok.DisplayLevel = maxRecLevel - tok.RecLevel;
            }
        }

        private void CalculateDisplayLevels() {
            int[] recLvs = new int[CursorPos];

            foreach (var tok in this.OrderBy(e => -e.RecLevel)) {
                var end = tok.EndPos;
                if (end == -1) end = CursorPos;
                var slice = new ArraySegment<int>(recLvs, tok.StartPos, end - tok.StartPos);
                tok.DisplayLevel = slice.Max();
                for (int i = tok.StartPos; i < end; i++) {
                    recLvs[i] = tok.DisplayLevel + 1;
                }
            }
        }

        // todo: public SortLevels
        public override string ToString() {
            return string.Join(" ", (object[])TreeRanges);
        }

        public IEnumerator<HistoryToken> GetEnumerator() {
            return TreeRanges.Where(e => !e.Name.StartsWith("\"")).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
