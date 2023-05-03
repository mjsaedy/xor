using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MdSyConsoleApps {

    static class ColorCon {

        public static void Write(ConsoleColor color, string format, params object[] arg) {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(format, arg);
            Console.ForegroundColor = prev;
        }

        //usage:  ColorCon.PrintChars("\n\t<c1>Stitch, tile, rotate, flip, scale, and convert</c> image files.", ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Yellow);
        static public void PrintChars(string s, ConsoleColor color1, ConsoleColor color2, ConsoleColor color3) {
            int i = 0;
            char c;
            ConsoleColor prev = Console.ForegroundColor;
            //ConsoleColor color1 = ConsoleColor.Red;
            //ConsoleColor color2 = ConsoleColor.Green;
            //ConsoleColor color3 = ConsoleColor.Blue;

            s = ReplaceCaseInsensitive(s, "<c1>", '\x0F'.ToString());
            s = ReplaceCaseInsensitive(s, "<c2>", '\x10'.ToString());
            s = ReplaceCaseInsensitive(s, "<c3>", '\x11'.ToString());
            s = ReplaceCaseInsensitive(s, "</c>", '\x12'.ToString());
            s = ReplaceCaseInsensitive(s, "<lt>", "<");
            s = ReplaceCaseInsensitive(s, "<gt>", ">");
            s = ReplaceCaseInsensitive(s, "<br>", "\n");

            int len = s.Length;
            for (i = 0; i < len; i++) {
                c = s[i];
                if (c == '\x0F') {
                    Console.ForegroundColor = color1;
                } else if (c == '\x10') {
                    Console.ForegroundColor = color2;
                } else if (c == '\x11') {
                    Console.ForegroundColor = color3;
                } else if (c == '\x12') {
                    Console.ForegroundColor = prev;
                } else if (c == '\n') {
                    Console.Write(Environment.NewLine);
                } else {
                    Console.Write(c);
                }
            }
        }


        public static void Print(ConsoleColor color, string s) {
            Write(color, s, new object[] { });
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] arg) {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(format, arg);
            Console.ForegroundColor = prev;
        }

        public static void PrintInfo(string s) {
            WriteLine(ConsoleColor.Green, s, new object[] { });
        }

        public static void PrintInfo(string format, params object[] arg) {
            WriteLine(ConsoleColor.Green, format, arg);
        }

        public static void PrintError(string s) {
            WriteLine(ConsoleColor.Red, s, new object[] { });
        }

        public static void PrintError(string format, params object[] arg) {
            WriteLine(ConsoleColor.Red, format, arg);
        }

        public static void PrintWarning(string s) {
            WriteLine(ConsoleColor.Yellow, s, new object[] { });
        }

        public static void PrintWarning(string format, params object[] arg) {
            WriteLine(ConsoleColor.Yellow, format, arg);
        }

        public static string ReplaceCaseInsensitive(string str, string findMe, string newValue) {
            return Regex.Replace(str,
                Regex.Escape(findMe),
                Regex.Replace(newValue, "\\$[0-9]+", @"$$$0"),
                RegexOptions.IgnoreCase);
        }

    }

}
