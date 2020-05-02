using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ParserLib {
    public abstract class Parser {
        abstract public ParserHistory Run(string input);
    }
}
