using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib {
    public class HistoryToken {
        public string Name { get; }
        public int StartPos { get; }
        public int EndPos { get; }
        public int RecLevel { get; }

        internal HistoryToken(ParserTreeToken tok) {
            Name = tok.name;
            StartPos = tok.startPos;
            EndPos = tok.endPos;
            RecLevel = tok.recLevel;
        }

        public override string ToString() {
            if (EndPos == -1) return $"{StartPos}:-({Name}, {RecLevel})";
            return $"{StartPos}:{EndPos}({Name}, {RecLevel})";
        }
    }
}
