{
  function flatDeep(arr, d){
    if(d==undefined)d = Infinity;
    return d>0 ? arr.reduce(function(a, v){return a.concat(Array.isArray(v) ? flatDeep(v, d - 1) : v);}, []) : arr.slice();
  }

  function str(arr){
    if(Array.isArray(arr))return flatDeep(arr).join('');
    return arr.toString();
  }

  var strings = {};
  function addStr(a){
    var r = str(a).split('').map(e=>e.charCodeAt(0).toString(16).padStart(2,'0')).join('');
    r = "_"+r+"_";
    strings[r] = str(a);
    return " "+r+" ";
  }

  function getStrs(){
    var r = "";
    for(var k in strings){
      r += `${k} = "${strings[k]}"\n`;
    }
    return r;
  }
}

Code = arr:Line* {return str(arr)+"\n"+getStrs();}
Line = _ n:i _ "::=" _ r:Choice _ ";" _ {return str(n)+" = "+str(r)+"\n";}

Choice = a:Sequence _ "|" _ b:Choice {return str(a)+" / "+str(b);} / Sequence
Sequence = Item+
Item = Regex / String / i

Regex "regex" = _ "/" a:[^/]+ "/" {return str(a);}
String "string" = _ "\"" a:[^"]* "\"" {return addStr(a);}

i "identifier" = _ [^/|"'=;:*+ \x00-\x20]+
_ "whitespace" = [ \t\n\r]*
