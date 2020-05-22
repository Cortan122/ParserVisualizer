using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ParserLib {
    /// <summary>
    /// Строит синтаксическое дерево и хранит каждую его версию
    /// </summary>
    public class ParserHistory : IEnumerable<HistoryEntry> {
        /// <summary>
        /// Внутренний стек, используемый для построения дерева
        /// </summary>
        private Stack<ParserTreeToken> stack = new Stack<ParserTreeToken>();
        /// <summary>
        /// Список узлов, которые сейчас есть в дереве
        /// </summary>
        private List<ParserTreeToken> state = new List<ParserTreeToken>();
        /// <summary>
        /// Предыдущая позиция конца узла
        /// </summary>
        private int prevPos = -1;

        /// <summary>
        /// Оригинальная грамматика, из которой делаются все остальные
        /// </summary>
        public string OriginalRtf { get; }
        /// <summary>
        /// Входная строка
        /// </summary>
        public string InputString { get; }

        /// <summary>
        /// Имена всех правил
        /// </summary>
        public IEnumerable<string> RuleNames => RtfBuilder.GetNames(OriginalRtf);

        /// <summary>
        /// Список всех старых деревьев
        /// </summary>
        private List<HistoryEntry> history = new List<HistoryEntry>();

        /// <summary>
        /// Делает копию поля state
        /// </summary>
        private HistoryToken[] CopyState() {
            return state.Select(e => e.Clone()).ToArray();
        }

        internal ParserHistory(string rtf, string input) {
            OriginalRtf = rtf;
            InputString = input;
        }

        /// <summary>
        /// Сохраняет текущее состояние в поле history
        /// </summary>
        private void SaveState() {
            var tokens = CopyState();
            var rtf = RtfBuilder.Build(OriginalRtf, stack);
            var r = new HistoryEntry(tokens, rtf);
            if (!r.isBroken) history.Add(r);
        }

        /// <summary>
        /// Добавляет узел в дерево.
        /// Принимает на вход строки от синтаксического анализатора.
        /// Здесь происходит основное построение дерева
        /// </summary>
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
                    if (t.Parent != null) t.Parent.ChildCount--;
                    var i = state.IndexOf(t);
                    state.RemoveRange(i, state.Count - i);
                }
            }

            SaveState();
        }

        /// <summary>
        /// Реализует интерфейс IEnumerable<HistoryToken>
        /// </summary>
        public IEnumerator<HistoryEntry> GetEnumerator() => history.GetEnumerator();
        /// <summary>
        /// Реализует интерфейс IEnumerable
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => history.GetEnumerator();
        /// <summary>
        /// Индексатор
        /// </summary>
        public HistoryEntry this[int i] => history[i];
    }
}
