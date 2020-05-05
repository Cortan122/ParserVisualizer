using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ParserLib {
    internal class ParserTreeToken {
        public string Parent { get; }
        public string Name { get; }
        public int Index { get; }
        public int StartPos { get; }
        public int RecLevel { get; }
        public int EndPos { get; set; }
        public Dictionary<string, int> Dict { get; }

        public ParserTreeToken(string parent, string name, int index, int startPos, int recLevel) {
            if (name.StartsWith('_') && name.EndsWith('_') && name.Length > 1) {
                var chars = Enumerable.Range(0, name.Length / 2 - 1)
                    .Select(i => (char)Convert.ToUInt16(name.Substring(i * 2 + 1, 2), 16));
                name = '"' + string.Join("", chars) + '"';
            }
            this.Parent = parent;
            this.Name = name;
            this.Index = index;
            this.StartPos = startPos;
            this.RecLevel = recLevel;
            this.EndPos = -1;
            this.Dict = new Dictionary<string, int>();
        }

        public override string ToString() {
            return $"{StartPos}:{EndPos}({Name}, {Index})";
        }

        public HistoryToken Clone() {
            return new HistoryToken(this);
        }
    }
}
