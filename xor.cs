using System;
using System.IO;
//using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MdSyConsoleApps {

    class XOREncrypt {

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        private static bool verbose = false;
        private static string exe_file_name;

        static void Main(string[] args) {

            IntPtr handle = GetConsoleWindow();

            exe_file_name = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location).ToLower();
            //exe_file_name = Path.GetFileName(Environment.GetCommandLineArgs()[0]); //could return "xor" or "xor.exe"

            int minimum_args_number = 2;

            CmdLineParser.CmdParser p = new CmdLineParser.CmdParser(args, true);
            bool only1k = p.HasArg("1k", "1024", "q", "quick");
            verbose = p.HasArg("v", "verbose");
            string key = p.GetValue("k", "key");

            //If the executable is renamed to "1k.exe" it will ignore all command line options and performs the 1k:(255-byte) 
            if (exe_file_name == "1k.exe") {
                minimum_args_number = 1;
                only1k = true;
                //verbose = false;
                key = "";
            }
            
            if (args.Length < minimum_args_number || p.HasArg("?", "h", "help")) {
                Help();
                return;
            }

            if (String.IsNullOrEmpty(key)) {
                if (!only1k) {
                    ColorCon.PrintError("\n A key must be provided when not using /quick");
                    Help();
                    return;
                }
            }
            p.Glob();
            if (p.Files.Length < 1) {
                ColorCon.PrintError("\n No files to work with!");
                return;
            }

            Console.WriteLine("\n Processing{0} {1} files:\n", only1k ? " first 1k of" : "", p.Files.Length);
            int numSuccess = 0, numFail = 0;
            foreach (string file in p.Files) {
                bool success;
                if(only1k){
                    success = XorFile_1k(file, key);
                } else {
                    success = XorFile(file, key);
                }
                if (success) {
                    Console.Write("  [ok]");
                    ColorCon.WriteLine(ConsoleColor.White, " {0}", Path.GetFileName(file));
                    if (verbose) Console.WriteLine();
                    numSuccess++;
                } else {
                    numFail++;
                }
                TaskbarProgress.SetValue(handle, numSuccess + numFail, p.Files.Length);
            }
            Console.WriteLine();
            ColorCon.WriteLine(ConsoleColor.Yellow, " Successfully processed {0} {1}", numSuccess, numSuccess == 1 ? "file" : "files");
            if (numFail > 0) {
                ColorCon.WriteLine(ConsoleColor.Red, " Failed to process {0} {1}", numFail, numFail == 1 ? "file" : "files");
            }
            TaskbarProgress.SetState(handle, TaskbarProgress.TaskbarStates.NoProgress);
        }

        static bool XorFile(string filename, string key){
            const int CHUNK_SIZE = 1024 * 100; //could be set to other values
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            int total_bytes = 0;
            try {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite)) {
                    while (true) {
                        var chunk = new byte[CHUNK_SIZE];
                        int bytesRead = fs.Read(chunk, 0, CHUNK_SIZE);
                        if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  reading...{0} bytes", bytesRead);
                        total_bytes += bytesRead;
                        if (bytesRead == 0) {
                            if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  ...");
                            break;
                        }
                        int chunkLen = bytesRead;
                        int keyLen = keyBytes.Length;
                        // use XOR to encrypt the chunk with the key
                        var encryptedChunk = new byte[chunkLen];
                        for (int i = 0; i < chunkLen; i++) {
                            encryptedChunk[i] = (byte)(chunk[i] ^ key[i % keyLen]);
                        }
                        // write the encrypted chunk back to the file
                        fs.Seek(-chunkLen, SeekOrigin.Current);
                        fs.Write(encryptedChunk, 0, chunkLen);
                        if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  writing...{0} bytes", chunkLen);
                    }
                }
                if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  total read/written: {0} bytes", total_bytes);
            } catch (Exception ex) {
                //ColorCon.WriteLine(ConsoleColor.Red, "\n ERROR {0} while processing {1}\n", ex.Message, filename);
                ColorCon.WriteLine(ConsoleColor.Red, "  [ERROR] {0}", ex.Message);
                return false;
            }
            return true;
        }

        static bool XorFile_1k(string filename, string key){
            // Process only the first 1024 bytes of the file
            byte[] data = new byte[1024];

            try {
                using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite)) {
                    int bytesRead = file.Read(data, 0, 1024);
                    if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  read {0} bytes", bytesRead);
                    byte[] encrypted;
                    if (String.IsNullOrEmpty(key)) {
                        encrypted = Mirror256(data);
                        if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  encrypting without key");
                    } else {
                        encrypted = XorEncryptData(data, key);
                        if (verbose) ColorCon.WriteLine(ConsoleColor.Green, "  encrypting with key=\"{0}\"", key);
                    }
                    // Write the encrypted data back to the file
                    file.Seek(0, SeekOrigin.Begin);
                    //file.Write(encrypted, 0, 1024);               <---- will write extra null bytes when file size is < 1k
                    //file.Write(encrypted, 0, encrypted.Length);   <---- will write extra null bytes when file size is < 1k
                    file.Write(encrypted, 0, bytesRead);
                    if (verbose) ColorCon.WriteLine(ConsoleColor.Cyan,
                                 "  " + ByteArrayToHex(data, 16) + " --> " + ByteArrayToHex(encrypted, 16));
                }
            } catch (Exception ex){
                //ColorCon.WriteLine(ConsoleColor.Red, "\n ERROR {0} while processing {1}\n", ex.Message, filename);
                ColorCon.WriteLine(ConsoleColor.Red, "  [ERROR] {0}", ex.Message);
                return false;
            }
            return true;
        }

        static byte[] XorEncryptData(byte[] data, string key){
            // Encrypt data using XOR with key
            byte[] encrypted = new byte[data.Length];
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            int keyLength = keyBytes.Length;
            for (int i = 0; i < data.Length; i++){
                encrypted[i] = (byte)(data[i] ^ keyBytes[i % keyLength]);
            }
            return encrypted;
        }

        static byte[] Mirror256(byte[] data){
            byte[] encrypted = new byte[data.Length];
            for (int i = 0; i < data.Length; i++){
                encrypted[i] = (byte)(255 - data[i]);
            }
            return encrypted;
        }

        private static string ByteArrayToHex(byte[] barray, int length) {
            int _length = (length <= 0 || length > barray.Length) ? barray.Length : length; //sanity check
            //char[] c = new char[barray.Length * 2];
            char[] c = new char[_length * 2];
            byte b;
            //for (int i = 0; i < barray.Length; ++i)
            for (int i = 0; i < _length; ++i) {
                b = ((byte)(barray[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(barray[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c);
        }


        static string[,] ArgHelp = {
                          { "/?", "/h /help",  "Display program help" },
                          { "/k:", "/key:",  "The string used as key in encryption or decryption" },
                          { "/v", "/verbose", "Verbose progress messages"},
                          { "/1k", "/1024 /q /quick", "Process only first 1024 bytes of the file"}
         };

        static void Help() {
            if (exe_file_name != "1k.exe") {
                ColorCon.WriteLine(ConsoleColor.White, "\n  XOR encrypt/decrypt files");
                Console.WriteLine("\n  Usage:");
                ColorCon.WriteLine(ConsoleColor.White, "  {0} /key:<encryption/decryption key> [/1k] [/v] filename1 [filename2 ...]", exe_file_name);
                Console.WriteLine();
                for (int i = 0; i <= ArgHelp.GetUpperBound(0); i++) {
                    Console.Write("  ");
                    for (int j = 0; j <= ArgHelp.GetUpperBound(1); j++) {
                        Console.Write(j == 0 ? "{0,-7}" : "{0,-24}", ArgHelp[i, j]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("\n    If /key is not provided and /quick is present, the first 1024 bytes will be changed to (255-byte)");
            } else if (exe_file_name == "1k.exe") {
                Console.WriteLine("\n\tUsage:\n\t{0}  filename1 [filename2 ...]", exe_file_name);
            } else {

            }
        }
    }
}
