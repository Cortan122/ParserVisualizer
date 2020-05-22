namespace ParserLib {
    /// <summary>
    /// ParserTreeToken но без служебных полей
    /// </summary>
    public class HistoryToken {
        /// <summary>
        /// Название правила, по которому был постоен этот узел
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Индекс первого символа, входящего в этот узел
        /// </summary>
        public int StartPos { get; }
        /// <summary>
        /// Индекс последнего символа, входящего в этот узел
        /// </summary>
        public int EndPos { get; }

        /// <summary>
        /// Глубина этого узла в дереве
        /// </summary>
        internal int RecLevel { get; }
        /// <summary>
        /// Можно ли этот узел убирать при обрезке дерева
        /// </summary>
        public bool Trimmable { get; }
        /// <summary>
        /// Визуальный уровень узла
        /// </summary>
        public int DisplayLevel { get; internal set; }

        internal HistoryToken(ParserTreeToken tok) {
            Name = tok.Name;
            StartPos = tok.StartPos;
            EndPos = tok.EndPos;
            RecLevel = tok.RecLevel;
            DisplayLevel = RecLevel;
            Trimmable = tok.ChildCount == 1 && tok.EndPos >= 0;
        }

        internal HistoryToken() { }

        /// <summary>
        /// Переопределяет метод класса Object
        /// </summary>
        public override string ToString() {
            if (EndPos == -1) return $"{StartPos}:-({Name}, {RecLevel})";
            return $"{StartPos}:{EndPos}({Name}, {RecLevel})";
        }
    }
}
