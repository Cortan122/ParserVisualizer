using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ParserLib {
    public class ParserHistory : IEnumerable<HistoryEntry> {
        private Stack<ParserTreeToken> stack = new Stack<ParserTreeToken>();
        private List<ParserTreeToken> state = new List<ParserTreeToken>();
        private int prevPos = -1;

        /// <summary>
        /// Inherited parser parameters.
        /// </summary>
        public string OriginalRtf { get; }
        public string InputString { get; }

        public IEnumerable<string> RuleNames => RtfBuilder.GetNames(OriginalRtf);

        private List<HistoryEntry> history = new List<HistoryEntry>();

        private HistoryToken[] CopyState() {
            return state.Select(e => e.Clone()).ToArray();
        }

        internal ParserHistory(string rtf, string input) {
            OriginalRtf = rtf;
            InputString = input;
        }

        private void SaveState() {
            var tokens = CopyState();
            var rtf = RtfBuilder.Build(OriginalRtf, stack);
            var r = new HistoryEntry(tokens, rtf);
            history.Add(r);
        }

        public void Add(string line) {
            if (line == null) return;
            line = line.Trim();
            if (line == "") return;

            if (line.StartsWith("eval failed: SyntaxError:")) {
                // todo
                return;
            }

            var words = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            var pos = int.Parse(words[0].Split(':').Last()) - 1;

            var hasFailed = prevPos > pos;
            while (prevPos > pos) {
                var t = state.Last();
                if (t.EndPos == -1) break;
                t.Parent.ChildCount--;
                state.RemoveAt(state.Count - 1);
                prevPos = t.EndPos;
            }
            prevPos = pos;

            if (hasFailed) SaveState();

            if (words[1] == "rule.enter") {
                var val = 0;
                ParserTreeToken parent = null;
                if (stack.Count != 0) {
                    parent = stack.Peek();
                    parent.ChildCount++;
                    var dict = stack.Peek().Dict;
                    dict.TryGetValue(words[2], out val);
                    dict[words[2]] = val + 1;
                }
                var t = new ParserTreeToken(parent, words[2], val, pos, stack.Count);
                state.Add(t);
                stack.Push(t);
            } else {
                var t = stack.Pop();
                t.EndPos = pos;
                if (words[1] != "rule.match") {
                    t.EndPos = -2;
                    t.Parent.ChildCount--;
                    var i = state.IndexOf(t);
                    state.RemoveRange(i, state.Count - i);
                }
            }

            SaveState();
        }

        public IEnumerator<HistoryEntry> GetEnumerator() => history.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => history.GetEnumerator();
        public HistoryEntry this[int i] => history[i];
    }
}
