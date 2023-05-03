using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace CmdLineParser {

    internal class NativeCommandLine {

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine) {
            string executableName;
            return CommandLineToArgs(commandLine, out executableName);
        }

        public static string[] CommandLineToArgs(string commandLine, out string executableName) {
            int argCount;
            IntPtr result;
            string arg;
            IntPtr pStr;
            result = CommandLineToArgvW(commandLine, out argCount);

            if (result == IntPtr.Zero) {
                throw new System.ComponentModel.Win32Exception();
            } else {
                try {
                    // Jump to location 0*IntPtr.Size (in other words 0).
                    pStr = Marshal.ReadIntPtr(result, 0 * IntPtr.Size);
                    executableName = Marshal.PtrToStringUni(pStr);

                    // Ignore the first parameter because it is the application
                    // name which is not usually part of args in Managed code.
                    string[] args = new string[argCount - 1];
                    for (int i = 0; i < args.Length; i++) {
                        pStr = Marshal.ReadIntPtr(result, (i + 1) * IntPtr.Size);
                        arg = Marshal.PtrToStringUni(pStr);
                        args[i] = arg;
                    }

                    return args;
                } finally {
                    Marshal.FreeHGlobal(result);
                }
            }
        }
    }

    internal class StringLogicalComparer : IComparer, IComparer<string> {
        
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string x, string y);
        
        public int Compare(object x, object y) {
            return StrCmpLogicalW(x.ToString(), y.ToString());
        }
        
        public int Compare(string x, string y) {
            return StrCmpLogicalW(x, y);
        }
    }



    public static class Utils {

        //ToDo: sort options...
        //for now, sort by extension then by natural filename
        public static string[] SortFileNames(string[] files) {
            string[] sorted;
            //sorted = files.Select(fn => new FileInfo(fn)).OrderBy(f => f.Name).Select(fi => fi.Name).ToArray(); //by name
            //sorted = files.Select(fn => new FileInfo(fn)).OrderBy(f => f.Name).Select(fi => fi.Extension).ToArray(); //by extension
            //sorted = files.Select(fn => new FileInfo(fn)).OrderBy(f => f.Name).Select(fi => fi.Length).ToArray(); //by size
            //sorted = files.Select(fn => new FileInfo(fn)).OrderBy(f => f.Name).Select(fi => fi.CreationTime).ToArray(); //by date

            //sorted = files.OrderBy(f => f).ToArray();                    //by name
            //sorted = files.OrderBy(f => Path.GetExtension(f)).ToArray(); //by extension

            //sorted = files.OrderBy(f => Path.GetExtension(f)).ThenBy(f => f).ToArray(); //extension then filename

            //sorted = files; Array.Sort(sorted, new StringLogicalComparer()); //natural sort (using full path)

            //sort by extension then by natural filename (`a1, a2, a11` rather than normal sort of `a1, a11, a2`)
            sorted = files.OrderBy(f => Path.GetExtension(f)).ThenBy(f => f, new StringLogicalComparer()).ToArray();
            return sorted;
        }


        //Extract the Directory name from a full file name, wildcards `?` and `*` are ok.
        //required, because `Path.GetDirectoryName()` will throw if path contains wildcards
        private static string getDirName(string FileName) {
            string s = "";
            int iPos = 0;
            iPos = FileName.LastIndexOf("\\");
            if (iPos == -1) {
                s = "";
            } else {
                s = FileName.Substring(0, iPos + 1);
            }
            return s;
        }


        public static string[] ValidateFiles(string[] files) {
            //or simply, using LINQ:    files = files.Where(x => File.Exists(x)).ToArray();
            List<string> list = new List<string>(files.Length);
            foreach (string f in files) {
                if (File.Exists(f)) {
                    list.Add(f);
                }
            }
            return list.ToArray();
        }


        public static string[] Unique(string[] a) {
            //using LINQ: string[] r = a.Distinct().ToArray();
            List<String> lst = new List<string>();
            foreach (string s in a) {
                if (!lst.Contains(s)) {
                    lst.Add(s);
                }
            }
            return lst.ToArray();
        }

        public static string[] Glob(string s) {
            //CmdLineParser.Glob(@"E:\[lock]\1\pdf\_Cool\4*.jpg");
            //CmdLineParser.Glob(@"4*.jpg");
            string[] files;
            if (s.IndexOf('?') > -1 || s.IndexOf('*') > -1) {
                string dir_name = getDirName(s);
                string pattern = "";
                if (String.IsNullOrEmpty(dir_name)) {
                    dir_name = Path.GetFullPath(".");
                    pattern = s;
                } else {
                    pattern = s.Replace(dir_name, "");
                }
                //Console.WriteLine(dir_name + " :: " + pattern);
                files = Directory.GetFiles(dir_name, pattern, SearchOption.TopDirectoryOnly);
                //Console.WriteLine(String.Join("\n", files));
            } else {
                files = null;
            }
            return files;
        }

        
        //Windows increments the first found (n)
        //for example the next name after `file (1) name (1).txt` is `file (2) name (1).txt`
        //I find it illogical to mess with anything but the last parentheses!
        public static string GetNextAvailableFileName(string fileName) {
            fileName = Path.GetFullPath(fileName);
            string path = Path.GetDirectoryName(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int number = 1;
            if (File.Exists(fileName)) {
                Match regex = Regex.Match(name, @"(.+)\s\((\d+)\)$");   //if the filename is in the form `blah blah (n)`
                if (regex.Success) {
                    name = regex.Groups[1].Value;
                    number = int.Parse(regex.Groups[2].Value);
                }
            }
            while (File.Exists(fileName)) {
                fileName = Path.Combine(path, name + " (" + number.ToString() + ")" + ext);
                number++;
            }
            return fileName;
        }

        public static string SuffixFileName(string filename, string suffix) {
            return AffixFileName("", filename, suffix);
        }

        public static string AffixFileName(string prefix, string filename, string suffix) {
            string dir = Path.GetDirectoryName(filename);
            string ext = Path.GetExtension(filename);
            string leaf = Path.GetFileNameWithoutExtension(filename);
            return Path.Combine(dir, prefix + leaf + suffix + ext);
        }
    
    
    }

    public class CmdParser {

        #region Public properties
        
        public const string SEPERATOR = "|";
        public bool IgnoreCase = false;
        public string[] Files, RawSwitches;
        public Dictionary<string, string> Switches = new Dictionary<string, string>();

        public string ExePath {
            get {
                return System.Reflection.Assembly.GetExecutingAssembly().Location.ToLower();
            }
        }

        public string ExeName {
            get {
                return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToLower();
            }
        }

        private string[,] _arghelp;
        public string[,] ArgHelp {
            get {
                return _arghelp;
            }

            set {
                _arghelp = value;
            }
        }

        #endregion

        #region Public static methods

        public static string[] CmdLineToArray(string cmdline) {
            //string cmdline = Environment.CommandLine;
            //CommandLineToArgvW() considers `\"` to be an escaped quote!
            //fix the glitch (is there any unwanted side effects?)
            Regex rgx = new Regex(@"([^\\])(\\"")", RegexOptions.None); //capture only `\"` but not `\\"`
            string s = rgx.Replace(cmdline, @"$1\\""");
            //Console.WriteLine(s);
            string[] b = NativeCommandLine.CommandLineToArgs(s);
            return b;
        }

        //parse args char by char
        //the value of the boolean does not matter, it is there just to distinguish the overloaded function!
        public static string[] CmdLineToArray(string cmdline, bool charBychar) {
            int qcount = 0;
            string x = "";
            List<string> a = new List<string>();

            for (int i = 0; i < cmdline.Length; i++) {
                x += cmdline[i];
                if (cmdline[i] == '"') {
                    qcount++;
                }
                if (cmdline[i] == ' ') { // || s[i] == '/'
                    if (qcount % 2 == 0) {
                        x = x.Trim();
                        if (x != "") a.Add(x);
                        x = "";
                    }
                }
            }
            //add remaining arg
            a.Add(x);
            return a.ToArray();
        }

        #endregion

        #region Ctor

        //the parameterless overload uses the fix automatically (and is case-insensitive)
        //remember that it is not usable if compiled as DLL
        public CmdParser()
            : this(CmdLineToArray(Environment.CommandLine), true) {
            this.IgnoreCase = true;
        }

        public CmdParser(string[] args, bool ignoreCase) {

            this.IgnoreCase = ignoreCase;

            List<string> fileList = new List<string>();
            List<string> switchList = new List<string>();

            foreach (string arg in args) {
                if (arg.StartsWith("-") || arg.StartsWith("/")) {
                    switchList.Add(arg);
                } else {
                    fileList.Add(arg);
                }
            }

            Files = fileList.ToArray();
            RawSwitches = switchList.ToArray();

            string k, v;
            foreach (string s in RawSwitches) {
                int pos = s.IndexOf(":");
                if (pos == -1) {
                    k = s.Substring(1); //remove first char (`-` or `/`)
                    v = "";
                } else {
                    k = s.Substring(1, pos - 1);
                    v = s.Substring(pos + 1);
                }
                if (IgnoreCase) {
                    k = k.ToLower();
                }
                if (Switches.ContainsKey(k)) {
                    //join (subject to change, maybe into a jagged array?)
                    Switches[k] = Switches[k] + SEPERATOR + v;
                } else {
                    Switches.Add(k, v);
                }
            }
        }

        #endregion

        #region Public methods

        //instance method to glob wildcards and validate file existence in `Files[]`
        public void Glob() {
            List<string[]> lst = new List<string[]>();
            foreach (var f in Files) {
                string[] a = Utils.Glob(f);
                if (a == null) {                 //no wildcards, so
                    lst.Add(new string[] { f }); //add filename as a one-element array
                } else {
                    lst.Add(a);                  //add globbed files
                }
            }
            //flattening using LINQ is easier: var flattenedUniqueValues = values.SelectMany(x => x).Distinct();
            List<string> lstFiles = new List<string>();
            foreach (string[] x in lst) {
                lstFiles.AddRange(x);
            }
            //lstFiles.ForEach(f => Path.GetFullPath(f));  <-- BUG!!!!!!!!! ForEach cannot directly modify array elements. The change is not reflected in the array.
            for (int i = 0; i < lstFiles.Count; i++) {
                lstFiles[i] = Path.GetFullPath(lstFiles[i]);
            }
            string[] files = lstFiles.ToArray();
            files = Utils.Unique(files);
            files = Utils.ValidateFiles(files);
            Files = files;
        }

        //1 arg
        public string GetValue(string arg) {
            /*
            * we can just use the 2-or-more args overload:
               return GetValue(arg, "");
           */
            if (IgnoreCase) {
                arg = arg.ToLower();
            }
            if (Switches.ContainsKey(arg)) {
                return Switches[arg];
            } else {
                return null;
            }
        }

        //2 or more args
        public string GetValue(params string[] args) {
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (IgnoreCase) {
                    arg = arg.ToLower();
                }
                if (Switches.ContainsKey(arg)) {
                    return Switches[arg];
                }
            }
            return null;
        }

        //1 arg
        public T GetValue<T>(string arg) {
            string s = GetValue(arg);
            return GetTypeValue<T>(s);
        }

        //2 or more args
        public T GetValue<T>(params string[] args) {
            string s = GetValue(args);
            return GetTypeValue<T>(s);
        }

        //safe to use even if string is not in a correct format
        public static T GetTypeValue<T>(string s) {
            double val;
            bool b = Double.TryParse(s, out val); //parse as double just in case of floating numbers...
            val = (b ? val : 0.0);
            T result;
            try {
                result = (T)Convert.ChangeType((object)val, typeof(T));
            } catch {
                result = default(T);
            }
            return result;
        }


        //throws if string is not in a correct format!
        /*public T GetValue<T>(string arg) {
            arg = GetValue(arg);
            if (string.IsNullOrEmpty(arg)) {
                return default(T);
            }
            return (T)Convert.ChangeType(arg, typeof(T));
        }*/

        //1 arg
        public bool HasArg(string arg) {
            if (IgnoreCase) {
                arg = arg.ToLower();
            }
            return Switches.ContainsKey(arg);
        }

        //2 or more args
        public bool HasArg(params string[] args) {
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (IgnoreCase) {
                    arg = arg.ToLower();
                }
                if (Switches.ContainsKey(arg)) {
                    return true;
                }
            }
            return false;
        }


        //1 arg
        public bool EatArg(string arg) {
            //return EatArg(new string[] { arg });
            if (IgnoreCase) {
                arg = arg.ToLower();
            }
            if (Switches.ContainsKey(arg)) {
                Switches.Remove(arg);
                return true;
            }
            return false;
        }

        //2 or more args
        public bool EatArg(params string[] args) {
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (IgnoreCase) {
                    arg = arg.ToLower();
                }
                if (Switches.ContainsKey(arg)) {
                    Switches.Remove(arg);
                    return true;
                }
            }
            return false;
        }

        public void ShowHelp(string usage) {
            Console.WriteLine(usage);
            for (int i = 0; i <= _arghelp.GetUpperBound(0); i++) {
                Console.Write("  ");
                for (int j = 0; j <= _arghelp.GetUpperBound(1); j++) {
                    Console.Write(j == 0 ? "{0,-7}" : "{0,-24}", ArgHelp[i, j]);
                }
                Console.WriteLine();
            }
        }

        #endregion

        #region Indexers

        //string indexer: returns arg values
        public string this[string k] {
            get {
                return GetValue(k);
            }
        }

        //int indexer: returns files (accepts negative index to access array from end!)
        public string this[int i] {
            get {
                if (i < 0) i += Files.Length;
                return Files[i]; //no bounds checking, if it's out of bound it will throw IndexOutOfRangeException
            }
        }

        #endregion

    }




    /*
    //Extension methods
    public static class Ex {
        public static bool IsNull(this object o) {
            return (o == null);
        }
    }*/

}
