using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MdsyApps {

    class MdsyConsoleApp {
        
        [STAThread]
        public static void Main(string[] args) {


            CmdLineParser.CmdParser p = new CmdLineParser.CmdParser(args, true);

            Console.WriteLine();
            //Console.WriteLine(p.ExePath);
            //Console.WriteLine(p.ExeName);
            //Console.WriteLine();

            if (args.Length > 1) {
                
            } else {
                
            }

            p.ArgHelp = new string[,] {
                          { "/?", "/h /help",  "Display program help" },
                          { "/k:", "/key:",  "<string> key used in encryption or decryption" },
                          { "/v", "/verbose", "Verbose progress messages"},
                          { "/1k", "/1024 /q /quick", "Process only first 1024 bytes of the file"}
            };

            p.ShowHelp(string.Format(" usage:\n {0} /? /k /v /q filename1 [filename2 ...]", p.ExeName));

            bool only1k = p.HasArg("1k", "1024", "q", "quick");
            bool verbose = p.HasArg("v", "verbose");
            string key = p.GetValue("k", "key");

            p.Glob();
            if (p.Files.Length < 1) {
                Console.WriteLine("\n No files to work with!");
                return;
            }
            //p.Files.Length
            foreach (string file in p.Files) {

            }




            //Console.WriteLine("\n\npress any key...\n");
            //Console.ReadKey();
        }


    } //class
} //namespace