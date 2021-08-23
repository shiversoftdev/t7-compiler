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
using System.Reflection;
using System.Net;

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
        private static string UpdatesURL = "https://gsc.dev/t7c_version";
        private static string UpdaterURL = "https://gsc.dev/t7c_updater";
        static void Main(string[] args)
        {
            try
            {
                string lv = GetEmbeddedVersion();
                Console.WriteLine($"T7 Compiler version {lv}, by Serious");
                ulong local_version = ParseVersion(lv);
                ulong remote_version = 0;
                Console.WriteLine($"Checking client version... (our version is {local_version:X})");
                using (WebClient client = new WebClient())
                {
                    string downloadString = client.DownloadString(UpdatesURL);
                    remote_version = ParseVersion(downloadString.ToLower().Trim());
                }
                if(local_version < remote_version)
                {
                    Console.WriteLine("Client out of date, downloading installer...");
                    string filename = Path.Combine(Path.GetTempPath(), "t7c_installer.exe");
                    if(File.Exists(filename)) File.Delete(filename);
                    using(WebClient client = new WebClient())
                    {
                        client.DownloadFile(UpdaterURL, filename);
                    }
                    Console.WriteLine("Installing update... Please wait for a confirmation window to pop up before attempting to inject again...");
                    Process.Start(filename, "--install_silent");
                    Environment.Exit(0);
                }
            }
            catch
            {
                // we dont care if we cant update tbf
                Console.WriteLine($"Error updating client... ignoring update");
            }


            Root root = new Root();
            if (args.Length > 0 && args[0] == "--build")
            {
                root.cmd_Compile(new string[] { "scripts", "pc", "t7", "false", "--build" });
                return;
            }
            
            root.AddCommand(ConsoleKey.Q, "Quit Program", root.cmd_Exit);
            root.AddCommand(ConsoleKey.H, "Hash String [fnv|fnv64|gsc] <baseline> <prime> [input]", root.cmd_HashString);
            root.AddCommand(ConsoleKey.T, "Toggle Text History", root.cmd_ToggleNoClear);
            root.AddCommand(ConsoleKey.C, "Compile Script [path]", root.cmd_Compile);
            root.AddCommand(ConsoleKey.I, "Inject Script [path]", root.cmd_Inject);
            while (true)
            {
                try { root.Exec(root.PrintOptions()); }
                catch(Exception e)
                {
                    root.Error(e.ToString());
                }
            }
        }

        static ulong ParseVersion(string vstr)
        {
            ulong result = 0;
            string[] numbers = vstr.Split('.');
            int index = 0;
            for(int i = 0; i < numbers.Length; i++, index++)
            {
                int real_index = numbers.Length - 1 - i;
                ulong num = ushort.Parse(numbers[real_index]);
                result += num << (index * 16);
            }
            return result;
        }

        static string GetEmbeddedVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DebugCompiler.version";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd().Trim().ToLower();
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

        private int cmd_Inject(string[] args)
        {
            if(args.Length != 1)
            {
                return Error("Invalid arguments. Please specify a file to inject.");
            }

            if(!File.Exists(args[0]))
            {
                return Error("Invalid arguments. Specified file does not exist.");
            }

            byte[] buffer = null;
            try
            {
                buffer = File.ReadAllBytes(args[0]);
            }
            catch
            {
                return Error("Failed to read the file specified");
            }

            PointerEx injresult = InjectScript(@"scripts/shared/duplicaterender_mgr.gsc", buffer);
            Console.WriteLine();
            Console.ForegroundColor = !injresult ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\t[{"scripts/shared/duplicaterender_mgr.gsc"}]: {(!injresult ? "Injected" : $"Failed to Inject ({injresult:X})")}\n");

            if (!injresult)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Press any key to reset gsc parsetree... If in game, you are probably going to crash.\n");
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.ReadKey(true);
                NoExcept(FreeScript);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tScript parsetree has been reset\n");
                Console.ForegroundColor = ConsoleColor.White;
            }
            return 0;
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

        private class SourceTokenDef
        {
            public string FilePath;
            public int LineStart;
            public int LineEnd;
            public int CharStart;
            public int CharEnd;
            public Dictionary<int, (int CStart, int CEnd)> LineMappings = new Dictionary<int, (int CStart, int CEnd)>();
        }

        private int cmd_Compile(string[] args)
        {
            if (args.Length < 1)
                return Error("Invalid arguments");

            if (!Directory.Exists(args[0]))
                return Error("Path is either not a directory or does not exist");

            Platforms platform = Platforms.PC;
            Games game = Games.T7;
            string source = "";
            CompiledCode code;
            List<SourceTokenDef> SourceTokens = new List<SourceTokenDef>();
            StringBuilder sb = new StringBuilder();
            int CurrentLineCount = 0;
            int CurrentCharCount = 0;
            foreach (string f in Directory.GetFiles(args[0], "*.gsc", SearchOption.AllDirectories))
            {
                var CurrentSource = new SourceTokenDef();
                CurrentSource.FilePath = f.Replace(args[0], "").Substring(1).Replace("\\", "/");
                CurrentSource.LineStart = CurrentLineCount;
                CurrentSource.CharStart = CurrentCharCount;
                foreach (var line in File.ReadAllLines(f))
                {
                    CurrentSource.LineMappings[CurrentLineCount] = (CurrentCharCount, CurrentCharCount + line.Length + 1);
                    sb.Append(line);
                    sb.Append("\n");
                    CurrentLineCount += 1;
                    CurrentCharCount += line.Length + 1; // + \n
                }
                CurrentSource.LineEnd = CurrentLineCount;
                CurrentSource.CharEnd = CurrentCharCount;
                // Console.WriteLine($"{CurrentSource.FilePath} start {CurrentSource.LineStart} end {CurrentSource.LineEnd}");
                SourceTokens.Add(CurrentSource);
                sb.Append("\n"); // remember that this is here because its going to fuck up irony
            }

            source = sb.ToString();
            var ppc = new ConditionalBlocks();
            List<string> conditionalSymbols = new List<string>();
            conditionalSymbols.Add("BO3");
            if (File.Exists("gsc.conf"))
            {
                foreach(string line in File.ReadAllLines("gsc.conf"))
                {
                    var split = line.Trim().Split('=');
                    if (split.Length < 2) continue;
                    switch(split[0].ToLower().Trim())
                    {
                        case "symbols":
                            foreach(string token in split[1].Trim().Split(','))
                            {
                                conditionalSymbols.Add(token);
                            }
                            break;
                    }
                }
            }
            ppc.LoadConditionalTokens(conditionalSymbols);
            
            try
            {
                source = ppc.ParseSource(source);
            }
            catch(CBSyntaxException e)
            {
                int errorCharPos = e.ErrorPosition;
                int numLineBreaks = 0;
                foreach(var stok in SourceTokens)
                {
                    do
                    {
                        if(errorCharPos < stok.CharStart || errorCharPos > stok.CharEnd)
                        {
                            break; // havent reached the target index set yet
                        }
                        // now we have the source file we want
                        errorCharPos -= numLineBreaks; // adjust for inserted linebreaks between files
                        foreach(var line in stok.LineMappings)
                        {
                            var constraints = line.Value;
                            if(errorCharPos < constraints.CStart || errorCharPos > constraints.CEnd)
                            {
                                continue; // havent found the index we want yet
                            }
                            // found the target line
                            return Error($"{e.Message} in scripts/{stok.FilePath} at line {line.Key - stok.LineStart}, position {errorCharPos - constraints.CStart}");
                        }
                    }
                    while (false);
                    numLineBreaks++;
                }
                return Error(e.Message);
            }

            code = Compiler.Compile(platform, game, Modes.MP, false, source);
            if (code.Error != null && code.Error.Length > 0)
            {
                if(code.Error.LastIndexOf("line=") < 0)
                {
                    return Error(code.Error);
                }
                int iStart = code.Error.LastIndexOf("line=") + "line=".Length;
                int iLength = code.Error.LastIndexOf("]") - iStart;
                int line = int.Parse(code.Error.Substring(iStart, iLength));
                // Console.WriteLine(code.Error + " :: " + line);
                foreach (var stok in SourceTokens)
                {
                    do
                    {
                        if(stok.LineStart <= line && stok.LineEnd >= line)
                        {
                            return Error($"Syntax error in scripts/{stok.FilePath} around line {line - stok.LineStart + 1}");
                        }
                    }
                    while (false);
                    line--; // acccount for linebreaks appended to each file
                }
                return Error(code.Error);
            }

            string cpath = "compiled.gsc";
            File.WriteAllBytes(cpath, code.CompiledScript);
            string hpath = "hashes.txt";
            StringBuilder hashes = new StringBuilder();
            foreach(var kvp in code.HashMap)
            {
                hashes.AppendLine($"0x{kvp.Key:X}, {kvp.Value}");
            }
            File.WriteAllText(hpath, hashes.ToString());
     
            Success(cpath);
            Success("Script compiled. Press I to inject or anything else to continue");

            if (args.Length < 5 && Console.ReadKey(true).Key != ConsoleKey.I)
                return 0;

            byte[] data = code.CompiledScript;

            if(game == Games.T7 && platform == Platforms.PC)
            {
                PointerEx injresult = InjectScript(@"scripts/shared/duplicaterender_mgr.gsc", code.CompiledScript);
                Console.WriteLine();
                Console.ForegroundColor = !injresult ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"\t[{"scripts/shared/duplicaterender_mgr.gsc"}]: {(!injresult ? "Injected" : $"Failed to Inject ({injresult:X})")}\n");

                if(!injresult)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Press any key to reset gsc parsetree... If in game, you are probably going to crash.\n");
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.ReadKey(true);
                    NoExcept(FreeScript);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\tScript parsetree has been reset\n");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            else
            {
                Error("Cannot inject to this platform");
            }

            return 0;
        }

        private void NoExcept(Action a)
        {
            try
            {
                a();
            }
            catch { }
        }

        private PointerEx llpModifiedSPTStruct = 0;
        private PointerEx llpOriginalBuffer;
        private int OriginalSourceChecksum;
        private int InjectedBuffSize;
        private T7SPT InjectedScript;
        private int OriginalPID = 0;
        private int InjectScript(string replacePath, byte[] buffer)
        {
            NoExcept(FreeScript);
            if(BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
            {
                return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops III");
            }
            ProcessEx bo3 = "blackops3";
            if (bo3 == null)
            {
                return Error("No game process found for black ops 3");
            }
            bo3.OpenHandle();
            OriginalPID = bo3.BaseProcess.Id;
            Console.WriteLine($"s_assetPool:ScriptParseTree => {bo3[0x9409AB0]}");
            var sptGlob = bo3.GetValue<ulong>(bo3[0x9409AB0]);
            var sptCount = bo3.GetValue<int>(bo3[0x9409AB0] + 0x14);
            var SPTEntries = bo3.GetArray<T7SPT>(sptGlob, sptCount);
            for(int i = 0; i < SPTEntries.Length; i++)
            {
                var entry = SPTEntries[i];
                if (!entry.llpName) continue;
                try
                {
                    // find target
                    var name = bo3.GetString(entry.llpName);
                    if(name.ToLower().Trim().Replace("\\", "/") == replacePath.ToLower().Trim().Replace("\\", "/"))
                    {
                        // cache target info
                        llpModifiedSPTStruct = (ulong)(i * Marshal.SizeOf(typeof(T7SPT))) + sptGlob;
                        llpOriginalBuffer = entry.lpBuffer;
                        OriginalSourceChecksum = bo3.GetValue<int>(llpOriginalBuffer + 0x8);
                        
                        // patch script into memory
                        entry.lpBuffer = bo3.QuickAlloc(buffer.Length);
                        BitConverter.GetBytes(OriginalSourceChecksum).CopyTo(buffer, 0x8);
                        bo3.SetBytes(entry.lpBuffer, buffer);

                        // patch spt struct
                        bo3.SetStruct(llpModifiedSPTStruct, entry);

                        // cache the struct data for uninjection
                        InjectedScript = entry;
                        InjectedBuffSize = buffer.Length;
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    continue;
                }
            }
            bo3.CloseHandle();
            return 2;
        }

        private void FreeScript()
        {
            if (!llpModifiedSPTStruct) return;
            ProcessEx bo3 = "blackops3";
            if (bo3 == null) return;
            if (bo3.BaseProcess.Id != OriginalPID) return;
            bo3.OpenHandle();

            // free allocated space
            ProcessEx.VirtualFreeEx(bo3.Handle, InjectedScript.lpBuffer, (uint)InjectedBuffSize, (int)EnvironmentEx.FreeType.Release);

            // Patch spt struct
            InjectedScript.lpBuffer = llpOriginalBuffer;
            bo3.SetStruct(llpModifiedSPTStruct, InjectedScript);
            bo3.CloseHandle();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct T7SPT
        {
            public PointerEx llpName;     //00
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
