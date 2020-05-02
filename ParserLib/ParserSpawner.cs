using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ParserLib {
    public class ParserSpawner : Parser {
        private string name;
        public ParserSpawner(string name) {
            this.name = name;
        }

        public override ParserHistory Run(string input) {
            var tree = new ParserHistory(File.ReadAllText("parsers/" + this.name + ".rtf"));

            Process process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, args) => {
                tree.Add(args.Data);
            };
            process.StartInfo.FileName = "parsers/" + this.name + ".exe";
            process.Start();
            process.BeginOutputReadLine();

            process.StandardInput.Write(input);
            process.StandardInput.Close();

            process.WaitForExit();
            return tree;
        }
    }
}
