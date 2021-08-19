using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExampleExternal
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.Exit(__main(args));
        }

        static int __main(string[] args)
        {
            ProcessEx proc = "Among Us";
            if (!proc) return Error("Process for Among Us not found...");
            var game = proc["GameAssembly"];
            proc.SetValue(game[0x12345], 0x123321);
            proc.SetArray(game[0x12345], new long[] { 0x1, 0x2, 0x3, 0x4 });
            return 0;
        }

        static int Error(string Message = "", int errorCode = 1)
        {
            ConsoleColor cached = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ForegroundColor = cached;
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            return errorCode;
        }
    }
}
