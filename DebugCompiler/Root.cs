using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreyarchCompiler;
using T7CompilerLib;
using TreyarchCompiler.Enums;
using T7CompilerLib.OpCodes;
using XDevkit;
using Microsoft.Test.Xbox.XDRPC;
using TreyarchCompiler.Utilities;
using System.Windows.Forms.VisualStyles;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using T7MemUtil;

namespace DebugCompiler
{
    class Root
    {
        private struct CommandInfo
        {
            internal string CommandName;
            internal CommandHandler Exec;
        }
        private delegate int CommandHandler(string[] args);
        private Dictionary<ConsoleKey, CommandInfo> CommandTable = new Dictionary<ConsoleKey, CommandInfo>();
        private bool ClearHistory = false;

        static void Main(string[] args)
        {
            Root root = new Root();
            if (args.Length > 0 && args[0] == "--build")
            {
                root.cmd_Compile(new string[] { "scripts", "pc", "t7", "false", "--build" });
                return;
            }
            
            root.AddCommand(ConsoleKey.Q, "Quit Program", root.cmd_Exit);
            root.AddCommand(ConsoleKey.H, "Hash String [fnv|fnv64|gsc] <baseline> <prime> [input]", root.cmd_HashString);
            root.AddCommand(ConsoleKey.T, "Toggle Text History", root.cmd_ToggleNoClear);
            root.AddCommand(ConsoleKey.C, "Compile Script [path] [pc] [T7]", root.cmd_Compile);

            while (true)
            {
                try { root.Exec(root.PrintOptions()); }
                catch(Exception e)
                {
                    root.Error(e.ToString());
                }
            }
        }

        private ConsoleKey PrintOptions()
        {
            if (ClearHistory)
                Console.Clear();

            foreach (var kvp in CommandTable)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value.CommandName}");
            }

            return Console.ReadKey(true).Key;
        }

        public static IEnumerable<String> ParseArgs(String line, Char delimiter, Char textQualifier)
        {

            if (line == null)
                yield break;

            else
            {
                Char prevChar = '\0';
                Char nextChar = '\0';
                Char currentChar = '\0';

                Boolean inString = false;

                StringBuilder token = new StringBuilder();

                for (int i = 0; i < line.Length; i++)
                {
                    currentChar = line[i];

                    if (i > 0)
                        prevChar = line[i - 1];
                    else
                        prevChar = '\0';

                    if (i + 1 < line.Length)
                        nextChar = line[i + 1];
                    else
                        nextChar = '\0';

                    if (currentChar == textQualifier && (prevChar == '\0' || prevChar == delimiter) && !inString)
                    {
                        inString = true;
                        continue;
                    }

                    if (currentChar == textQualifier && (nextChar == '\0' || nextChar == delimiter) && inString)
                    {
                        inString = false;
                        continue;
                    }

                    if (currentChar == delimiter && !inString)
                    {
                        yield return token.ToString();
                        token = token.Remove(0, token.Length);
                        continue;
                    }

                    token = token.Append(currentChar);

                }

                yield return token.ToString();

            }
        }

        private void AddCommand(ConsoleKey key, string CmdName = "Unknown Command", CommandHandler cex = null)
        {
            if (CommandTable.ContainsKey(key) || cex == null)
                return;
            CommandTable[key] = new CommandInfo() { CommandName = CmdName, Exec = cex };
        }

        private int Exec(ConsoleKey cmd)
        {
            if (!CommandTable.ContainsKey(cmd))
                return 1;

            Success(CommandTable[cmd].CommandName);
            Console.WriteLine("Enter args (if any):");
            string args = Console.ReadLine().Trim();
            Success(args);

            int ret = CommandTable[cmd].Exec.Invoke(ParseArgs(args, ' ', '"').ToArray());
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(false);
            Console.WriteLine();
            return ret;
        }

        private int Error(string msg = "Error encountered")
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
            Console.WriteLine();
            return 1;
        }

        private int Success(string msg = "")
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
            Console.WriteLine();
            return 0;
        }

        #region commands

        private static Dictionary<uint, string> t8_dword;
        private static Dictionary<uint, string> t7_dword;
        private static Dictionary<ulong, string> t8_qword;
        private static void LoadHashTable(bool force = false)
        {
           
        }

        private int cmd_DumpEmptySlots(string[] args)
        {
            return -1;
        }

        private int cmd_migrateMap(string[] args)
        {
            return -1;
        }

        private int cmd_ExtractStrings(string[] args)
        {
            return -1;
        }

        public unsafe static string DecodeAscii(byte[] buffer, int index = 0)
        {
            fixed (byte* bytes = &buffer[index])
            {
                return new string((sbyte*)bytes);
            }
        }

        private int cmd_Collect(string[] args)
        {
            return -1;
        }

        private int cmd_Automap(string[] args)
        {
            return -1;
        }

        private int cmd_HashString(string[] args)
        {
            if (args.Length != 2 && args.Length != 4)
                return Error("Invalid arguments");

            string input;

            string method = args[0].Trim().ToLower();

            switch (method)
            {
                case "fnv64":
                    ulong fnv64Offset = 14695981039346656037;
                    ulong fnv64Prime = 0x100000001b3;

                    if (args.Length == 2)
                        input = args[1].Replace('"', ' ').Trim();
                    else
                    {
                        if (args.Length != 4)
                            return Error("Invalid arguments");
                        try
                        {
                            fnv64Offset = ulong.Parse(args[1].Trim().ToLower().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                            fnv64Prime = ulong.Parse(args[2].Trim().ToLower().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                            input = args[3].Replace('"', ' ').Trim();
                        }
                        catch
                        {
                            return Error("Invalid arguments");
                        }
                    }

                    Console.WriteLine(HashFNV1a(Encoding.ASCII.GetBytes(input), fnv64Offset, fnv64Prime).ToString("X8"));
                    return 0;

                case "fnv":
                    uint baseline = 0x4B9ACE2F;
                    uint prime = 0x1000193;

                    if (args.Length == 2)
                        input = args[1].Replace('"', ' ').Trim();
                    else
                    {
                        if (args.Length != 4)
                            return Error("Invalid arguments");
                        try
                        {
                            baseline = uint.Parse(args[1].Trim().ToLower().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                            prime = uint.Parse(args[2].Trim().ToLower().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                            input = args[3].Replace('"', ' ').Trim();
                        }
                        catch
                        {
                            return Error("Invalid arguments");
                        }
                    }

                    Console.WriteLine(Com_Hash(input, baseline, prime).ToString("X4"));
                    return 0;


                default:
                    return Error($"Invalid method '{method}'");
            }
        }


        private int cmd_GenerateHashMap(string[] args)
        {
            return -1;
        }

        private int cmd_Permute(string[] args)
        {
            return -1;
        }

        private int cmd_StatDump(string[] args)
        {
            return -1;
        }

        private string TryGetHash(string tok)
        {
            if (!long.TryParse(tok, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long resultant))
                return tok;
            if (t8_qword.TryGetValue((ulong)resultant, out string dehashed)) return dehashed;
            return tok;
        }

        private int cmd_Exit(string[] args)
        {
            Environment.Exit(0);
            return 0;
        }

        private int cmd_ToggleNoClear(string[] args)
        {
            ClearHistory = !ClearHistory;

            Console.WriteLine($"Console history {(!ClearHistory ? "enabled" : "disabled")}.");

            return 0;
        }

        private int cmd_MapFileNS(string[] args)
        {
            return -1;
        }

        private int cmd_IncludeMapper(string[] args)
        {
            return -1;
        }

        private int cmd_Compile(string[] args)
        {
            if (args.Length < 3)
                return Error("Invalid arguments");

            if (!Directory.Exists(args[0]))
                return Error("Path is either not a directory or does not exist");

            if (!Enum.TryParse(args[1], true, out Platforms platform))
                return Error("Invalid arguments: Platform invalid");

            if (!Enum.TryParse(args[2], true, out Games game))
                return Error("Invalid arguments: Game invalid");

            string source = "";
            CompiledCode code;

            foreach(string f in Directory.GetFiles(args[0], "*.gsc", SearchOption.AllDirectories))
            {
                source += File.ReadAllText(f) + "\n";
            }

            code = Compiler.Compile(platform, game, Modes.MP, false, source);

            if (code.Error != null && code.Error.Length > 0)
            {
                return Error(code.Error);
            }

            string cpath = "compiled.gsc";

            File.WriteAllBytes(cpath, code.CompiledScript);
     
            Success(cpath);
            Success("Script compiled. Press I to inject or anything else to continue");

            if (args.Length < 5 && Console.ReadKey(true).Key != ConsoleKey.I)
                return 0;

            byte[] data = code.CompiledScript;

            if(game == Games.T7 && platform == Platforms.PC)
            {
                bool injresult = T7MemUtil.T7Memory.PatchGSCScript(@"scripts/shared/duplicaterender_mgr.gsc", code.CompiledScript);
                Console.WriteLine();
                Console.ForegroundColor = injresult ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"\t[{"scripts/shared/duplicaterender_mgr.gsc"}]: {(injresult ? "Injected" : "Failed to Inject")}\n");

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Press any key to reset gsc parsetree... If in game, you are probably going to crash.\n");
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.ReadKey(true);
                T7Memory.FreeAll();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tScript parsetree has been reset\n");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Error("Cannot inject to this platform");
            }

            return 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct T7SPT
        {
            public PointerEx lpName;     //00
            public int BuffSize;       //08
            public int Pad;
            public PointerEx lpBuffer;//10
        };
        
        private int cmd_Dump(string[] args)
        {
            return -1;
        }

#endregion

        uint Com_Hash(string Input, uint IV, uint XORKEY)
        {
            uint hash = IV;

            foreach (char c in Input)
                hash = (char.ToLower(c) ^ hash) * XORKEY;

            hash *= XORKEY;

            return hash;
        }

        public ulong HashFNV1a(byte[] bytes, ulong fnv64Offset, ulong fnv64Prime)
        {
            ulong hash = fnv64Offset;

            for (var i = 0; i < bytes.Length; i++)
            {
                hash = hash ^ bytes[i];
                hash *= fnv64Prime;
            }

            return hash;
        }

        private ulong FS_HashFileName(string input, ulong hashSize)
        {
            return 0;
        }

        private static void SerializeMetaOps()
        {
        }

        private static Dictionary<byte, ScriptOpCode> XboxCodes = null;
        
    }
}
