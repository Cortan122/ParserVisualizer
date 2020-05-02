using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib {
    public class ParserHistory : IEnumerable<HistoryEntry> {
        private Stack<ParserTreeToken> stack = new Stack<ParserTreeToken>();
        private List<ParserTreeToken> state = new List<ParserTreeToken>();
        private string rawRtf;
        private int prevPos = -1;

        private List<HistoryEntry> history = new List<HistoryEntry>();

        private HistoryToken[] CopyState() {
            return state.Select(e => e.Clone()).ToArray();
        }

        internal ParserHistory(string rtf) {
            rawRtf = rtf;
        }

        public void Add(string line) {
            if (line == null) return;
            line = line.Trim();
            if (line == "") return;

            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var pos = int.Parse(words[0].Split(':').Last()); // -1

            while (prevPos > pos) {
                var t = state.Last();
                state.RemoveAt(state.Count - 1);
                prevPos = t.endPos;
            }
            prevPos = pos;

            if (words[1] == "rule.enter") {
                var val = 0;
                string parent = null;
                if (stack.Count != 0) {
                    parent = stack.Peek().name;
                    var dict = stack.Peek().dict;
                    dict.TryGetValue(words[2], out val);
                    dict[words[2]] = val + 1;
                }
                var t = new ParserTreeToken(parent, words[2], val, pos, stack.Count);
                state.Add(t);
                stack.Push(t);
            } else {
                var t = stack.Pop();
                t.endPos = pos;
                if (words[1] != "rule.match") {
                    t.endPos = -2;
                    var i = state.IndexOf(t);
                    state.RemoveRange(i, state.Count - i);
                }
            }

            history.Add(new HistoryEntry(CopyState(), RtfBuilder.Build(rawRtf, stack)));
        }

        public IEnumerator<HistoryEntry> GetEnumerator() => history.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => history.GetEnumerator();
        public HistoryEntry this[int i] => history[i];
    }
}
