using System.Diagnostics;
using System.IO;

namespace ParserLib {
    /// <summary>
    /// Запуск синтаксического анализатора
    /// </summary>
    public class Parser {
        /// <summary>
        /// Название синтаксического анализатора
        /// </summary>
        private string name;
        public Parser(string name) {
            this.name = name;
        }

        /// <summary>
        /// Запускает синтаксический анализатор 
        /// </summary>
        public ParserHistory Run(string input) {
            var rtf = File.ReadAllText("parsers/" + name + ".rtf");
            var tree = new ParserHistory(rtf, input);

            var process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, args) => {
                tree.Add(args.Data);
            };
            process.StartInfo.FileName = "parsers/" + name + ".exe";
            process.Start();
            process.BeginOutputReadLine();

            process.StandardInput.Write(input);
            process.StandardInput.Close();

            process.WaitForExit();
            return tree;
        }
    }
}
