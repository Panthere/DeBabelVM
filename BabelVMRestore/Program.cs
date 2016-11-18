using BabelVMRestore.Core;
using BabelVMRestore.Logger;
using BabelVMRestore.Utilities;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace BabelVMRestore
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Babel 8.x.x.x VM Restorer - LaPanthere @ rtn-team.cc";
            Console.WriteLine(@"______    ______       _          _ _   ____  ___");
            Console.WriteLine(@"|  _  \   | ___ \     | |        | | | | |  \/  |");
            Console.WriteLine(@"| | | |___| |_/ / __ _| |__   ___| | | | | .  . |");
            Console.WriteLine(@"| | | / _ \ ___ \/ _` | '_ \ / _ \ | | | | |\/| |");
            Console.WriteLine(@"| |/ /  __/ |_/ / (_| | |_) |  __/ \ \_/ / |  | |");
            Console.WriteLine(@"|___/ \___\____/ \__,_|_.__/ \___|_|\___/\_|  |_/");
            Console.WriteLine("                                 V1.2 - LaPanthere");


            
            try
            {
                Settings.LoadSettings(args);

            }
            catch (Exception)
            {
                
                ConsoleLogger.Error("[!] Error: Cannot load the file. Make sure it's a valid .NET file!");
                ConsoleLogger.Error("[!] Verbose mode can be activated with -v");
                return;
            }

            ConsoleLogger.Success("[!] Trying to Restore Methods from VM - for best results move VM Restore to target folder!");



            Environment.CurrentDirectory = Path.GetDirectoryName(Settings.FileName);

            MethodRestorer mr = new MethodRestorer(Settings.Module);
            mr.Restore();
            mr.Write(Settings.OutputFileName);


            Console.ReadKey();

        }
       
    }

}
