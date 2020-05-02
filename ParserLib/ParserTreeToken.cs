using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ParserLib {
    internal class ParserTreeToken {
        public readonly string parent;
        public readonly string name;
        public readonly int index;
        public readonly int startPos;
        public readonly int recLevel;
        public int endPos;
        public readonly Dictionary<string, int> dict;

        public ParserTreeToken(string parent, string name, int index, int startPos, int recLevel) {
            this.parent = parent;
            this.name = name;
            this.index = index;
            this.startPos = startPos;
            this.recLevel = recLevel;
            this.endPos = -1;
            this.dict = new Dictionary<string, int>();
        }

        public override string ToString() {
            return $"{startPos}:{endPos}({name}, {index})";
        }

        public HistoryToken Clone() {
            return new HistoryToken(this);
        }
    }
}
