{
  function flatDeep(arr, d){
    if(d==undefined)d = Infinity;
    return d>0 ? arr.reduce(function(a, v){return a.concat(Array.isArray(v) ? flatDeep(v, d - 1) : v);}, []) : arr.slice();
  }

  function color(hex){
    if(hex[0] == '#')hex = hex.slice(1);
    var v1 = parseInt(hex.slice(0,2),16);
    var v2 = parseInt(hex.slice(2,4),16);
    var v3 = parseInt(hex.slice(4,6),16);
    return "\\red"+v1+"\\green"+v2+"\\blue"+v3+";";
  }

  function colortbl(arr){
    return "{\\colortbl ;" + arr.map(color).join('') + "}";
  }

  function str(arr){
    if(Array.isArray(arr))return flatDeep(arr).join('');
    return arr.toString();
  }
}

Code = arr:Line* {
  var colortable = colortbl(["#725a7a", "#355c7d", "#c56c86", "#ff7582"]);
  var header = "{\\rtf1\\ansi{\\fonttbl\\f0\\fcharset1 Consolas;}\n"+colortable+"\\f0\\cf4\n";
  return header + str(arr).replace(/\n/g,'\\par\n') + "}";
}
Line = _ i _ "::=" _ Choice _ ";" _

Choice = f:Sequence arr:(_ "|" _ Choice) {return "\\strike{}" + str(f) + "\\strike0 " + str(arr);} / Sequence
Sequence = (_ Item)+
Item = Regex / e:String {return "\\b{}" + str(e) + "\\b0 ";} / e:i {return "\\b{}" + str(e) + "\\b0 ";}

Regex "regex" = a:_ arr:("/" [^/]+ "/") {return a+"\\cf3 " + str(arr) + "\\cf4 ";}
String "string" = a:_ arr:("\"" [^"]* "\"") {return a+"\\cf1 " + str(arr) + "\\cf4 ";}

i "identifier" = a:_ arr:[^/|"'=;:*+ \x00-\x20]+ {return a+"\\cf2 " + str(arr) + "\\cf4 ";}
_ "whitespace" = [ \t\n\r]* {return text();}
