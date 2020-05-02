using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KурсачLib {
  public class HistoryEntry {
    public HistoryToken[] TreeRanges { get; }
    public string RtfGrammar { get; }

    internal HistoryEntry(HistoryToken[] ranges, string rtf) {
      RtfGrammar = rtf;
      TreeRanges = ranges;
    }

    // todo: public SortLevels
    public override string ToString(){
      return string.Join(' ', (object[])TreeRanges);
    }
  }
}
