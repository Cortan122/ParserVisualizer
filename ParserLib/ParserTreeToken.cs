using System;
using System.Collections.Generic;
using System.Linq;

namespace ParserLib {
    /// <summary>
    /// Узел синтаксического дерева
    /// </summary>
    internal class ParserTreeToken {
        /// <summary>
        /// Родитель
        /// </summary>
        public ParserTreeToken Parent { get; }
        /// <summary>
        /// Название правила, по которому был постоен этот узел
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Каким по счету ребёнком является этот узел
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Индекс первого символа, входящего в этот узел
        /// </summary>
        public int StartPos { get; }
        /// <summary>
        /// Глубина этого узла в дереве
        /// </summary>
        public int RecLevel { get; }
        /// <summary>
        /// Индекс последнего символа, входящего в этот узел
        /// </summary>
        public int EndPos { get; set; }
        /// <summary>
        /// Количество детей
        /// </summary>
        public int ChildCount { get; set; }
        /// <summary>
        /// Количество детей каждого типа
        /// </summary>
        public Dictionary<string, int> Dict { get; }

        public ParserTreeToken(ParserTreeToken parent, string name, int index, int startPos, int recLevel) {
            if (name.StartsWith("_") && name.EndsWith("_") && name.Length > 1) {
                var chars = Enumerable.Range(0, name.Length / 2 - 1)
                    .Select(i => (char)Convert.ToUInt16(name.Substring(i * 2 + 1, 2), 16));
                name = '"' + string.Join("", chars) + '"';
            }
            Parent = parent;
            Name = name;
            Index = index;
            StartPos = startPos;
            RecLevel = recLevel;
            EndPos = -1;
            Dict = new Dictionary<string, int>();
        }

        /// <summary>
        /// Переопределяет метод класса Object
        /// </summary>
        public override string ToString() {
            return $"{StartPos}:{EndPos}({Name}, {Index})";
        }

        /// <summary>
        /// Делает копию этого узла, выбрасывая служебные поля
        /// </summary>
        public HistoryToken Clone() {
            return new HistoryToken(this);
        }
    }
}
