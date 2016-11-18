using BabelVMRestore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelVMRestore.Logger
{
    public class ConsoleLogger
    {
        public static ConsoleColor DefaultColor = ConsoleColor.White;

        public static void Verbose(string text, params object[] data)
        {
            if (!Settings.Verbose)
                return;

            Console.ForegroundColor = ConsoleColor.Yellow;
            WriteLine(text, data);
            Console.ForegroundColor = DefaultColor;
        }
        public static void Error(string text, params object[] data)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine(text, data);
            Console.ForegroundColor = DefaultColor;
        }
        public static void Info(string text, params object[] data)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteLine(text, data);
            Console.ForegroundColor = DefaultColor;
        }
        public static void Success(string text, params object[] data)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLine(text, data);
            Console.ForegroundColor = DefaultColor;
        }
        public static void WriteLine(string text, params object[] data)
        {
            // TODO: Log file generation?
            Console.WriteLine(text, data);
        }
    }
}
