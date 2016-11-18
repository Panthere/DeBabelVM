using BabelVMRestore.Logger;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelVMRestore.Utilities
{
    public static class Settings
    {
        public static bool Verbose;
        public static string FileName;
        public static string OutputFileName;

        public static ModuleDefMD Module;

        public static void LoadSettings(string[] args)
        {
            // Input filename, verbose
            FileName = args[0];
            if (args.Length > 1)
                Verbose = args[1].ToLower() == "-v";

            // Module setting
            Module = ModuleDefMD.Load(FileName);

            // Output filepath name
            string dirName = Path.GetDirectoryName(args[0]);
            if (!dirName.EndsWith("\\"))
                dirName += "\\";
            OutputFileName = string.Format("{0}{1}_patched{2}",  dirName, Path.GetFileNameWithoutExtension(FileName), Path.GetExtension(FileName));

            ConsoleLogger.Success("[!] Loaded Module: {0}", Module.FullName);
            ConsoleLogger.Success("[!] Output File: {0}", OutputFileName);
        }
    }
}
