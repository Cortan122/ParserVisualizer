using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace KурсачLib {
  internal class RtfBuilder {
    private string[] lines;

    private RtfBuilder(string rtf){
      lines = rtf.Split('\n'); //.Where(e=>e.StartsWith("\\cf")).ToArray();
    }

    private void HighlightIdentifier(ParserTreeToken t){
      if(t.parent == null)return;
      var lineIndex = Array.FindIndex(lines, e=>e.StartsWith("\\cf2 "+t.parent));
      var i = 0;
      lines[lineIndex] = Regex.Replace(lines[lineIndex], @"\\b\{\}\\cf2 "+t.name, m=>{
        if(i++ == t.index)return @"\b \cf2 "+t.name;
        return @"\cf2 "+t.name;
      });
      lines[lineIndex] = lines[lineIndex].Replace("\\b{}","");
    }

    private string End(){
      return string.Join('\n', lines).Replace("{}","0");
    }

    public static string Build(string rtf, IEnumerable<ParserTreeToken> tokens){
      var builder = new RtfBuilder(rtf);
      foreach(var tok in tokens){
        builder.HighlightIdentifier(tok);
      }
      return builder.End();
    }
  }
}
