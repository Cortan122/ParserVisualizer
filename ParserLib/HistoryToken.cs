namespace ParserLib {
    public class HistoryToken {
        public string Name { get; }
        public int StartPos { get; }
        public int EndPos { get; }

        internal int RecLevel { get; }
        public bool Trimmable { get; }
        public int DisplayLevel { get; internal set; }

        internal HistoryToken(ParserTreeToken tok) {
            Name = tok.Name;
            StartPos = tok.StartPos;
            EndPos = tok.EndPos;
            RecLevel = tok.RecLevel;
            DisplayLevel = RecLevel;
            Trimmable = tok.ChildCount == 1 && tok.EndPos >= 0;
        }

        public override string ToString() {
            if (EndPos == -1) return $"{StartPos}:-({Name}, {RecLevel})";
            return $"{StartPos}:{EndPos}({Name}, {RecLevel})";
        }
    }
}
