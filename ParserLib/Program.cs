using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLib {
    class Program {
        static void Main() {
            Console.WriteLine("Main");
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Parser parser = new ParserSpawner("simple");
            var sw = new Stopwatch();
            sw.Start();
            var tree = parser.Run("1+1");
            sw.Stop();
            foreach (var item in tree) {
                Console.WriteLine(item);
            }
            Console.WriteLine(sw.Elapsed);
        }
    }
}
