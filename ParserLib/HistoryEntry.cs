using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ParserLib {
    /// <summary>
    /// Синтаксическое дерево в какой-то момент времени
    /// </summary>
    public class HistoryEntry : IEnumerable<HistoryToken> {
        /// <summary>
        /// Узлы дерева
        /// </summary>
        private HistoryToken[] treeRanges;
        /// <summary>
        /// Описание формальной грамматики в формате RTF
        /// </summary>
        public string RtfGrammar { get; }
        /// <summary>
        /// Индекс последнего анализированного символа
        /// </summary>
        public int CursorPos { get; }

        /// <summary>
        /// Сломано ли это дерево и надо ли его отображать
        /// </summary>
        internal bool isBroken = false;
        /// <summary>
        /// Надо ли подравнивать это дерево
        /// </summary>
        private bool isTrimmed = false;
        /// <summary>
        /// Ветки дерева
        /// </summary>
        private Dictionary<HistoryToken, HistoryToken> edges;

        internal HistoryEntry(HistoryToken[] ranges, string rtf) {
            RtfGrammar = rtf;
            if (ranges.Length == 0) {
                ranges = new HistoryToken[] { new HistoryToken() };
                isBroken = true;
            }
            treeRanges = ranges;
            CursorPos = treeRanges.Max(e => Math.Max(e.StartPos, e.EndPos - 1)) + 1;
        }

        /// <summary>
        /// Меняет настройки
        /// </summary>
        public void SetSettings(bool trim, bool orientation, bool gravity) {
            isTrimmed = trim;
            CalculateDisplayLevels(orientation ^ gravity);
            if (gravity) InvertDisplayLevels();
            edges = null;
        }

        /// <summary>
        /// Находит список всех веток дерева, но возвращает словарь, где ключ это узел, а значение это его родитель
        /// </summary>
        public Dictionary<HistoryToken, HistoryToken> GetEdges() {
            if (this.edges != null) return this.edges;
            var edges = new Dictionary<HistoryToken, HistoryToken>();
            var stacks = new HistoryToken[CursorPos];
            foreach (var tok in this.OrderBy(e => -e.RecLevel)) {
                var end = tok.EndPos;
                if (end == -1) end = CursorPos;

                for (var i = tok.StartPos; i < end; i++) {
                    if (stacks[i] != null) {
                        edges[stacks[i]] = tok;
                    }
                    stacks[i] = tok;
                }
            }
            this.edges = edges;
            return edges;
        }

        /// <summary>
        /// Переворачивает дерево
        /// </summary>
        private void InvertDisplayLevels() {
            var maxDisplayLevel = this.Max(e => e.DisplayLevel);
            foreach (var tok in this) {
                tok.DisplayLevel = maxDisplayLevel - tok.DisplayLevel;
            }
        }

        /// <summary>
        /// Рассчитывает гравитацию
        /// </summary>
        private void CalculateDisplayLevels(bool orientation = false) {
            var recLvs = new int[CursorPos];

            IEnumerable<HistoryToken> t = this.OrderBy(e => -e.RecLevel);
            if (orientation) t = t.Reverse();
            foreach (var tok in t) {
                var end = tok.EndPos;
                if (end == -1) end = CursorPos;

                var slice = new ArraySegment<int>(recLvs, tok.StartPos, end - tok.StartPos);
                tok.DisplayLevel = slice.Max();

                for (var i = tok.StartPos; i < end; i++) {
                    recLvs[i] = tok.DisplayLevel + 1;
                }
            }
        }

        /// <summary>
        /// Переопределяет метод класса Object
        /// </summary>
        public override string ToString() {
            return string.Join(" ", (object[])treeRanges);
        }

        /// <summary>
        /// Реализует интерфейс IEnumerable<HistoryToken>
        /// </summary>
        public IEnumerator<HistoryToken> GetEnumerator() {
            var t = treeRanges.Where(e => !e.Name.StartsWith("\""));
            if (isTrimmed) t = t.Where(e => !e.Trimmable);
            return t.GetEnumerator();
        }

        /// <summary>
        /// Реализует интерфейс IEnumerable
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
