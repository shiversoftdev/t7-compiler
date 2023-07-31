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
using T89CompilerLib;

namespace DebugCompiler
{
    class Root
    {
        private struct CommandInfo
        {
            internal string CommandName;
            internal CommandHandler Exec;
        }
        private delegate int CommandHandler(string[] args, string[] opts);
        private Dictionary<ConsoleKey, CommandInfo> CommandTable = new Dictionary<ConsoleKey, CommandInfo>();
        private bool ClearHistory = false;
        private static string UpdatesURL = "https://gsc.dev/t7c_version";
        private static string UpdaterURL = "https://gsc.dev/t7c_updater";
        private static string motdpath => Path.Combine(Application.StartupPath, "motd");
        private const int motdHrsRemindClear = 4; // number of hours between reminding users about the message of the day.
        static void motd()
        {
            var fi = new FileInfo(motdpath);
            if (fi.Exists)
            {
                if((DateTime.Now - fi.LastWriteTimeUtc).TotalMinutes <= (60 * motdHrsRemindClear))
                {
                    return; // we dont want to spam users with artificial delays in the program. Lets be nice and only show the motd once every 4 hours.
                }
            }
            File.WriteAllText(motdpath, "https://www.youtube.com/anthonything");
            fi = new FileInfo(motdpath);
            fi.LastWriteTimeUtc = DateTime.Now;
            Console.WriteLine($"Message of the Day:\n\tEver wanted to shoot your friend with a thundergun?\n\tEver wondered what would happen if you could 1v1 with the origins staffs?\n\tNow you can! Zombie Blood Rush is a Black Ops III zombies mod that lets you kill other players.\n\tYour points are your health. Kill other players and zombies to race to 100K points. Play now: https://steamcommunity.com/sharedfiles/filedetails/?id=2696008055\n\n");
            System.Threading.Thread.Sleep(4000);
        }
        static int Main(string[] args)
        {
            ParseCmdArgs(args, out string[] arguments, out string[] options);

            string lv = GetEmbeddedVersion();
            Console.WriteLine($"T7/T8 Compiler version {lv}, by Serious\n");

            if (!options.Contains("--noupdate"))
            {
                try
                {
                    motd();
                    ulong local_version = ParseVersion(lv);
                    ulong remote_version = 0;
                    Console.WriteLine($"Checking client version... (our version is {local_version:X})");
                    using (WebClient client = new WebClient())
                    {
                        string downloadString = client.DownloadString(UpdatesURL);
                        remote_version = ParseVersion(downloadString.ToLower().Trim());
                    }
                    if (local_version < remote_version)
                    {
                        Console.WriteLine("Client out of date, downloading installer...");
                        string filename = Path.Combine(Path.GetTempPath(), "t7c_installer.exe");
                        if (File.Exists(filename)) File.Delete(filename);
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(UpdaterURL, filename);
                        }
                        Console.WriteLine("Installing update... Please wait for a confirmation window to pop up before attempting to inject again...");
                        Process.Start(filename, "--install_silent");
                        return 0;
                    }
                } catch
                {
                    // we dont care if we cant update tbf
                    Console.WriteLine($"Error updating client... ignoring update");
                }
            }


            Root root = new Root();
            if (options.Contains("--build") || options.Contains("--compile"))
            {
                return root.cmd_Compile(arguments, options);
            }

            if (args.Length > 2 && options.Contains("--inject"))
            {
                return root.cmd_Inject(args, options);
            }

            root.AddCommand(ConsoleKey.Q, "Quit Program", root.cmd_Exit);
            root.AddCommand(ConsoleKey.H, "Hash String [fnv|fnv64|gsc] <baseline> <prime> [input]", root.cmd_HashString);
            root.AddCommand(ConsoleKey.T, "Toggle Text History", root.cmd_ToggleNoClear);
            root.AddCommand(ConsoleKey.C, "Compile Script [path] <T7|T8>", root.cmd_Compile);
            root.AddCommand(ConsoleKey.I, "Inject Script [path] <T7|T8> <inject path>", root.cmd_Inject);
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

        private static void ParseCmdArgs(string[] argv, out string[] arguments, out string[] options)
        {
            List<string> opts = new List<string>();
            List<string> args = new List<string>();

            foreach (string arg in argv)
            {
                if (arg == null || arg.Length == 0)
                {
                    continue;
                }

                if (arg[0] != '-')
                {
                    args.Add(arg);
                } else
                {
                    opts.Add(arg);
                }
            }

            arguments = args.ToArray();
            options = opts.ToArray();
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

            ParseCmdArgs(ParseArgs(args, ' ', '"').ToArray(), out string[] arguments, out string[] options);
            int ret = CommandTable[cmd].Exec.Invoke(arguments, options);
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

        private int cmd_Inject(string[] args, string[] opts)
        {
            if(args.Length < 1)
            {
                return Error("Invalid arguments. Please specify a file to inject.");
            }

            if(!File.Exists(args[0]))
            {
                return Error("Invalid arguments. Specified file does not exist.");
            }
            var cfg = new CompilerConfig();

            if (args.Length > 1)
            {
                if (!Enum.TryParse(args[1], true, out Games game))
                {
                    cfg.Game = Games.T7;
                }
                else
                {
                    cfg.Game = game;
                }
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
            var path = args.Length > 2 ? args[2] : (cfg.Game == Games.T7 ? @"scripts/shared/duplicaterender_mgr.gsc" : @"scripts/zm_common/load.gsc");
            PointerEx injresult = InjectScript(path, buffer, cfg, false);
            Console.WriteLine();
            Console.ForegroundColor = !injresult ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\t[{path}]: {(!injresult ? "Injected" : $"Failed to Inject ({injresult:X})")}\n");

            if (!injresult)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Press any key to reset gsc parsetree... If in game, you are probably going to crash.\n");
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.ReadKey(true);
                NoExcept(FreeActiveScript);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tScript parsetree has been reset\n");
                Console.ForegroundColor = ConsoleColor.White;
            }
            return 0;
        }

        private int cmd_DumpEmptySlots(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_migrateMap(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_ExtractStrings(string[] args, string[] opts)
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

        private int cmd_Collect(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_Automap(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_HashString(string[] args, string[] opts)
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


        private int cmd_GenerateHashMap(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_Permute(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_StatDump(string[] args, string[] opts)
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

        private int cmd_Exit(string[] args, string[] opts)
        {
            Environment.Exit(0);
            return 0;
        }

        private int cmd_ToggleNoClear(string[] args, string[] opts)
        {
            ClearHistory = !ClearHistory;

            Console.WriteLine($"Console history {(!ClearHistory ? "enabled" : "disabled")}.");

            return 0;
        }

        private int cmd_MapFileNS(string[] args, string[] opts)
        {
            return -1;
        }

        private int cmd_IncludeMapper(string[] args, string[] opts)
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

        private struct InjectionPoint
        {
            public byte[] ByteCode;
            public bool Client;
        }

        private class CompilerConfig
        {
            internal List<string> ConditionalSymbols { get; set; }  = new List<string>();
            internal Platforms Platform { get; set; } = Platforms.PC;
            internal Games Game { get; set; } = Games.T7;
            internal hotmode Hot { get; set; } = hotmode.none;
            internal bool Noruntime { get; set; }  = false;
            internal bool BuildScript { get; set; }  = false;
            internal bool CompileOnly { get; set; }  = false;
            internal bool InjectClient { get; set; }  = false;
            internal bool InjectServer { get; set; }  = true;
            internal bool InjectDLL { get; set; } = false;
            internal bool DllBuiltins { get; set; } = false;
            internal bool DllDetours { get; set; } = false;
            internal bool DllLazyLink { get; set; } = false;
            internal string ReplaceScript { get; set; }  = null;
            internal string ReplaceScriptClient { get; set; }  = null;
            internal string ScriptLocation { get; set; }  = "scripts";

            internal void ReadConfig(string line)
            {
                var split = line.Trim().Split('=');
                if (split.Length < 2) return;
                switch (split[0].ToLower().Trim())
                {
                    case "symbols":
                        foreach (string token in split[1].Trim().Split(','))
                        {
                            ConditionalSymbols.Add(token);
                        }
                        break;
                    case "script":
                        ReplaceScript = split[1].ToLower().Trim().Replace("\\", "/");
                        break;
                    case "script_client":
                        ReplaceScriptClient = split[1].ToLower().Trim().Replace("\\", "/");
                        break;
                    case "scriptlocation":
                        ScriptLocation = split[1];
                        break;
                    case "client":
                        InjectClient = split[1].ToLower().Trim() == "true";
                        break;
                    case "server":
                        InjectServer = split[1].ToLower().Trim() == "true";
                        break;
                    case "dll":
                        InjectDLL = split[1].ToLower().Trim() == "true";
                        break;
                    case "dll.lazylink":
                        DllLazyLink = split[1].ToLower().Trim() == "true";
                        break;
                    case "dll.detours":
                        DllDetours = split[1].ToLower().Trim() == "true";
                        break;
                    case "dll.builtins":
                        DllBuiltins = split[1].ToLower().Trim() == "true";
                        break;
                    case "game":
                        if (!Enum.TryParse(split[1].ToLower().Trim().Replace("\\", "/"), true, out Games game))
                        {
                            Game = Games.T7;
                        } else
                        {
                            Game = game;
                        }
                        break;
                    case "hot":
                        if (!Enum.TryParse(split[1].ToLower().Trim(), true, out hotmode hot))
                        {
                            Hot = hotmode.none;
                        } else
                        {
                            Hot = hot;
                        }
                        break;
                    case "noruntime":
                        Noruntime = split[1].ToLower().Trim() == "true";
                        break;
                }
            }
        }

        private int cmd_Compile(string[] args, string[] opts)
        {
            CompilerConfig cfg = new CompilerConfig();
            if (args.Length > 0)
            {
                cfg.ScriptLocation = args[0];
            }


            foreach (string opt in opts)
            {
                if (opt == "--build" || opt == "-b")
                {
                    cfg.BuildScript = true;
                } else if (opt == "--compile" || opt == "-c")
                {
                    cfg.CompileOnly = true;
                } else if (opt.Length > 2 && opt[1] == 'D')
                { // -Dsomething
                    cfg.ConditionalSymbols.Add(opt.Substring(2));
                } else if (opt.Length > 2 && opt[1] == 'C')
                { // -Coption=value
                    cfg.ReadConfig(opt.Substring(2));
                }
            }

            if (args.Length > 1)
            {
                try
                {
                    cfg.Game = (Games)Enum.Parse(typeof(Games), args[1], true);
                } catch { }
            }

            if (File.Exists("gsc.conf"))
            {
                foreach (string line in File.ReadAllLines("gsc.conf"))
                {
                    if (line.Trim().StartsWith("#")) continue;
                    cfg.ReadConfig(line);
                }
            }

            // add custom symbol to control GSC/CSC script compilation
            if (cfg.InjectClient)
            {
                cfg.ConditionalSymbols.Add("_INJECT_CLIENT");
            }

            if (cfg.InjectServer)
            {
                cfg.ConditionalSymbols.Add("_INJECT_SERVER");
            }

            if (!Directory.Exists(cfg.ScriptLocation))
                return Error($"Script location is either not a directory or does not exist {args[0]}");

            bool isT7 = cfg.Game == Games.T7;
            cfg.ReplaceScript = cfg.ReplaceScript ?? (isT7 ? @"scripts/shared/duplicaterender_mgr.gsc" : @"scripts/zm_common/load.gsc");
            cfg.ReplaceScriptClient = cfg.ReplaceScriptClient ?? "scripts/zm_common/load.csc";

            string hpath = "hashes.txt";
            StringBuilder hashes = new StringBuilder();
            List<InjectionPoint> bytecode = new List<InjectionPoint>();

            if (!cfg.InjectServer && !cfg.InjectClient)
            {
                return Error("Inject server and inject client are both set to false");
            }

            foreach (bool client in new bool[] { true, false })
            {
                if (client)
                {
                    if (!cfg.InjectClient)
                    {
                        continue;
                    }
                    if (cfg.Game != Games.T8)
                    {
                        return Error("Can't Inject client outside of Black Ops 4");
                    }
                } else
                {
                    if (!cfg.InjectServer)
                    {
                        continue;
                    }
                }

                string source = "";
                CompiledCode code;
                List<SourceTokenDef> SourceTokens = new List<SourceTokenDef>();
                StringBuilder sb = new StringBuilder();
                int CurrentLineCount = 0;
                int CurrentCharCount = 0;
                foreach (string f in Directory.GetFiles(cfg.ScriptLocation, client ? "*.csc" : "*.gsc", SearchOption.AllDirectories))
                {
                    var CurrentSource = new SourceTokenDef();
                    CurrentSource.FilePath = f.Replace(cfg.ScriptLocation, "").Substring(1).Replace("\\", "/");
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
                cfg.ConditionalSymbols.Add(isT7 ? "BO3" : "BO4");
                ppc.LoadConditionalTokens(cfg.ConditionalSymbols);

                try
                {
                    source = ppc.ParseSource(source);
                } catch (CBSyntaxException e)
                {
                    int errorCharPos = e.ErrorPosition;
                    int numLineBreaks = 0;
                    foreach (var stok in SourceTokens)
                    {
                        do
                        {
                            if (errorCharPos < stok.CharStart || errorCharPos > stok.CharEnd)
                            {
                                break; // havent reached the target index set yet
                            }
                            // now we have the source file we want
                            errorCharPos -= numLineBreaks; // adjust for inserted linebreaks between files
                            foreach (var line in stok.LineMappings)
                            {
                                var constraints = line.Value;
                                if (errorCharPos < constraints.CStart || errorCharPos > constraints.CEnd)
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

                code = Compiler.Compile(cfg.Platform, cfg.Game, Modes.MP, false, source);
                if (code.Error != null && code.Error.Length > 0)
                {
                    if (code.Error.LastIndexOf("line=") < 0)
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
                            if (stok.LineStart <= line && stok.LineEnd >= line)
                            {
                                return Error($"Syntax error in scripts/{stok.FilePath} around line {line - stok.LineStart + 1}");
                            }
                        }
                        while (false);
                        line--; // acccount for linebreaks appended to each file
                    }
                    return Error(code.Error);
                }

                string cpath = $"compiled.{(client ? (code.RequiresGSI ? "csic" : "cscc") : (code.RequiresGSI ? "gsic" : "gscc"))}";
                File.WriteAllBytes(cpath, code.CompiledScript);
                foreach (var kvp in code.HashMap)
                {
                    hashes.AppendLine($"0x{kvp.Key:X}, {kvp.Value}");
                }

                if (code.OpcodeEmissions != null)
                {
                    byte[] opsRaw = new byte[code.OpcodeEmissions.Count * 4];
                    for (int i = 0; i < code.OpcodeEmissions.Count; i++)
                    {
                        BitConverter.GetBytes(code.OpcodeEmissions[i]).CopyTo(opsRaw, i * 4);
                    }
                    File.WriteAllBytes(client ? "compiledclient.omap" : "compiled.omap", opsRaw);
                }
                Success(cpath);

                bytecode.Add(new InjectionPoint {
                    ByteCode = code.CompiledScript,
                    Client = client
                });
            }
            File.WriteAllText(hpath, hashes.ToString());

            if (cfg.CompileOnly)
            {
                return Success("Script compiled.");
            } else if (cfg.BuildScript)
            {
                Success("Script compiled. Injecting...");
            } else
            {
                Success("Script compiled. Press I to inject or anything else to continue");

                if (Console.ReadKey(true).Key != ConsoleKey.I)
                    return 0;
            }

            bool injected = false; ;

            foreach (var bc in bytecode)
            {
                byte[] data = bc.ByteCode;

                string rscript = bc.Client ? cfg.ReplaceScriptClient : cfg.ReplaceScript;
                PointerEx injresult = InjectScript(rscript, data, cfg, bc.Client);
                Console.WriteLine();
                Console.ForegroundColor = !injresult ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"\t[{rscript}]: {(!injresult ? "Injected" : $"Failed to Inject ({injresult:X})")}\n");

                injected = injected || !injresult;
            }

            if (injected && cfg.Hot == hotmode.none)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Press any key to reset gsc parsetree... If in game, you are probably going to crash.\n");
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.ReadKey(true);
                NoExcept(FreeActiveScript);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tScript parsetree has been reset\n");
                Console.ForegroundColor = ConsoleColor.White;
            }

            return 0;
        }

        private void FreeActiveScript()
        {
            switch(LastGameInjected)
            {
                case Games.T7:
                    NoExcept(FreeT7Script);
                    break;
                case Games.T8:
                    NoExcept(FreeT8Script);
                    break;
            }
        }

        private void NoExcept(Action a)
        {
            try
            {
                a();
            }
            catch { }
        }

        private Games LastGameInjected;
        private PointerEx llpModifiedSPTStruct = 0;
        private PointerEx llpOriginalBuffer;
        private int OriginalSourceChecksum;
        private int InjectedBuffSize;
        private T7SPT InjectedScript;
        private int OriginalPID = 0;
        private int InjectScript(string replacePath, byte[] buffer, CompilerConfig cfg, bool client)
        {
            LastGameInjected = cfg.Game;
            switch (cfg.Game)
            {
                case Games.T7: return InjectT7(replacePath, buffer, cfg.Hot, cfg.Noruntime);
                case Games.T8: return InjectT8(replacePath, buffer, cfg, client);
            }
            return Error("Invalid game provided to inject.");
        }

        private class GSICInfo
        {
            public List<T7ScriptObject.ScriptDetour> Detours = new List<T7ScriptObject.ScriptDetour>();

            public byte[] PackDetours()
            {
                List<byte> data = new List<byte>();
                foreach(var detour in Detours)
                {
                    data.AddRange(detour.Serialize());
                }
                return data.ToArray();
            }
        }

        private class GSICInfoT8
        {
            public List<T89ScriptObject.ScriptDetour> Detours = new List<T89ScriptObject.ScriptDetour>();

            public byte[] PackDetours()
            {
                List<byte> data = new List<byte>();
                foreach (var detour in Detours)
                {
                    data.AddRange(detour.Serialize());
                }
                return data.ToArray();
            }
        }

        private enum hotmode
        {
            none,
            csc,
            gsc
        }

        private int InjectT7(string replacePath, byte[] buffer, hotmode hot, bool noruntime)
        {
            NoExcept(FreeT7Script);
            GSICInfo gsi = null;
            if (BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
            {
                string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                if(preamble != "GSIC")
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops III.");
                }
                using(MemoryStream ms = new MemoryStream(buffer))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    T7ScriptObject.GSIFields currentField = T7ScriptObject.GSIFields.Detours;
                    reader.BaseStream.Position += 4;
                    gsi = new GSICInfo();
                    for(int numFields = reader.ReadInt32(); numFields > 0; numFields--)
                    {
                        currentField = (T7ScriptObject.GSIFields)reader.ReadInt32();
                        switch(currentField)
                        {
                            case T7ScriptObject.GSIFields.Detours:
                                int numdetours = reader.ReadInt32();
                                for(int j = 0; j < numdetours; j++)
                                {
                                    T7ScriptObject.ScriptDetour detour = new T7ScriptObject.ScriptDetour();
                                    detour.Deserialize(reader);
                                    gsi.Detours.Add(detour);
                                }
                                break;
                        }
                    }
                    buffer = buffer.Skip((int)reader.BaseStream.Position).ToArray();
                }
                if(BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops III.");
                }
            }
            ProcessEx bo3 = "blackops3";
            if (bo3 == null)
            {
                return Error("No game process found for Black Ops III.");
            }
            bo3.OpenHandle();
            bo3.SetDefaultCallType(ExCallThreadType.XCTT_QUAPC);
            OriginalPID = bo3.BaseProcess.Id;
            Console.WriteLine($"s_assetPool:ScriptParseTree => {bo3[0x9407AB0]}");
            var sptGlob = bo3.GetValue<ulong>(bo3[0x9407AB0]);
            var sptCount = bo3.GetValue<int>(bo3[0x9407AB0 + 0x14]);
            var SPTEntries = bo3.GetArray<T7SPT>(sptGlob, sptCount);
            for (int i = 0; i < SPTEntries.Length; i++)
            {
                var entry = SPTEntries[i];
                if (!entry.llpName) continue;
                try
                {
                    // find target
                    var name = bo3.GetString(entry.llpName);
                    if (hot != hotmode.none || name.ToLower().Trim().Replace("\\", "/") == replacePath.ToLower().Trim().Replace("\\", "/"))
                    {
                        // cache target info
                        if(hot == hotmode.none)
                        {
                            llpModifiedSPTStruct = (ulong)(i * Marshal.SizeOf(typeof(T7SPT))) + sptGlob;
                            llpOriginalBuffer = entry.lpBuffer;
                            OriginalSourceChecksum = bo3.GetValue<int>(llpOriginalBuffer + 0x8);
                        }
                        

                        // patch script into memory
                        entry.lpBuffer = bo3.QuickAlloc(buffer.Length);
                        BitConverter.GetBytes(OriginalSourceChecksum).CopyTo(buffer, 0x8);
                        bo3.SetBytes(entry.lpBuffer, buffer);

                        // patch spt struct
                        if(hot == hotmode.none)
                        {
                            bo3.SetStruct(llpModifiedSPTStruct, entry);

                            // cache the struct data for uninjection
                            InjectedScript = entry;
                            InjectedBuffSize = buffer.Length;
                        }
                        
                        if(!noruntime)
                        {
                            try
                            {
                                string exeFilePath = Assembly.GetExecutingAssembly().Location;
                                var result = bo3.Call<long>(bo3.GetProcAddress(@"kernel32.dll", @"LoadLibraryA"), Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll"));
                                Console.WriteLine($"LoadLibrary Result => {result:X}");

                                bo3.Refresh();
                                if (result <= 0)
                                {
                                    return (int)result;
                                }

                                bo3.Call<VOID>(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));
                                if (gsi != null)
                                {
                                    // detours
                                    if (gsi.Detours.Count > 0)
                                    {
                                        bo3.Call<VOID>(bo3.GetProcAddress(@"t7cinternal.dll", @"RegisterDetours"), gsi.PackDetours(), gsi.Detours.Count, (long)entry.lpBuffer);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                return 3;
                            }
                        }
                        
                        if(hot != hotmode.none)
                        {
                            // grab the asm buffer from resources, map and execute
                            var assembly = Assembly.GetExecutingAssembly();
                            var resourceName = "DebugCompiler.hotload.dat";
                            byte[] hot_fn = new byte[0];
                            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                            {
                                hot_fn = new byte[stream.Length];
                                stream.Read(hot_fn, 0, (int)stream.Length);
                            }
                            var hFnHotload = bo3.QuickAlloc(hot_fn.Length, true);
                            bo3.SetBytes(hFnHotload, hot_fn);

                            byte[] error_data = new byte[4];
                            try
                            {
                                bool result = bo3.Call<bool>(hFnHotload, entry.lpBuffer, (hot == hotmode.csc) ? 1 : 0, error_data);

                                if (!result)
                                {
                                    int error = BitConverter.ToInt32(error_data, 0);
                                    switch (error)
                                    {
                                        case 1:
                                            Console.WriteLine("HOTLOAD: Invalid script");
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Successfully hotloaded script!");
                                }
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }

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

        private T8InjectCache InjectCache = new T8InjectCache();
        private T8InjectCache InjectCacheClient = new T8InjectCache();

        private int InjectT8(string replacePath, byte[] buffer, CompilerConfig cfg, bool client)
        {
            if (client)
            {
                NoExcept(FreeT8ScriptClient);
            } else
            {
                NoExcept(FreeT8ScriptServer);
            }
            GSICInfoT8 gsi = null;
            if (BitConverter.ToInt64(buffer, 0) != 0x36000A0D43534780)
            {
                string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                if (preamble != "GSIC")
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops III.");
                }
                using (MemoryStream ms = new MemoryStream(buffer))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    T89ScriptObject.GSIFields currentField = T89ScriptObject.GSIFields.Detours;
                    reader.BaseStream.Position += 4;
                    gsi = new GSICInfoT8();
                    for (int numFields = reader.ReadInt32(); numFields > 0; numFields--)
                    {
                        currentField = (T89ScriptObject.GSIFields)reader.ReadInt32();
                        switch (currentField)
                        {
                            case T89ScriptObject.GSIFields.Detours:
                                int numdetours = reader.ReadInt32();
                                for (int j = 0; j < numdetours; j++)
                                {
                                    T89ScriptObject.ScriptDetour detour = new T89ScriptObject.ScriptDetour();
                                    detour.Deserialize(reader);
                                    gsi.Detours.Add(detour);
                                }
                                break;
                        }
                    }
                    buffer = buffer.Skip((int)reader.BaseStream.Position).ToArray();
                }
                if (BitConverter.ToInt64(buffer, 0) != 0x36000A0D43534780)
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops 4.");
                }
            }
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                return Error("No game process found for Black Ops 4.");
            }

            bo4.OpenHandle();
            OriginalPID = bo4.BaseProcess.Id;
            Console.WriteLine($"s_assetPool:ScriptParseTree => {bo4[0x912ABB0]}");
            var sptGlob = bo4.GetValue<ulong>(bo4[0x912ABB0]);
            var sptCount = bo4.GetValue<int>(bo4[0x912ABB0 + 0x14]);
            var SPTEntries = bo4.GetArray<T8SPT>(sptGlob, sptCount);
            replacePath = replacePath.ToLower().Trim().Replace("\\", "/");
            var surrogateScript = T8s64Hash(replacePath); // script we are hooking
            ulong targetScript; // script we are replacing

            if (client)
            {
                targetScript = 0x10aeb2e4f2b455a1;
            } else
            {
                targetScript = 0x124cecff7280be52;
            }
            T8InjectCache cache;

            if (client)
            {
                cache = InjectCacheClient;
            } else
            {
                cache = InjectCache;
            }


            cache.hSurrogate = 0;
            cache.hTarget = 0;

            for (int i = 0; i < SPTEntries.Length; i++)
            {
                var spt = SPTEntries[i];
                if (spt.ScriptName == surrogateScript)
                {
                    cache.Surrogate = spt;
                    cache.hSurrogate = sptGlob + (ulong)(i * Marshal.SizeOf(typeof(T8SPT)));
                }
                if (spt.ScriptName == targetScript)
                {
                    cache.Target = spt;
                    cache.hTarget = sptGlob + (ulong)(i * Marshal.SizeOf(typeof(T8SPT)));
                }
                if(cache.hSurrogate && cache.hTarget)
                {
                    break;
                }
            }

            try
            {
                if (!cache.hSurrogate || !cache.hTarget)
                {
                    return Error("Unable to identify critical injection information. Double check your script path, and try restarting the game. Make sure you are injecting in the pregame lobby.");
                }

                int includeOff = 0x58;
                int tableOff = 0x18;

                // patch include
                byte includeCount = bo4.GetValue<byte>(cache.Surrogate.Buffer + includeOff);
                PointerEx includeTable = cache.Surrogate.Buffer + bo4.GetValue<int>(cache.Surrogate.Buffer + tableOff);
                for (int i = 0; i < includeCount; i++)
                {
                    if (bo4.GetValue<ulong>(includeTable + (i * 8)) == targetScript)
                    {
                        goto patchBuff;
                    }
                }
                bo4.SetValue(includeTable + (includeCount * 8), targetScript);
                bo4.SetValue(cache.Surrogate.Buffer + includeOff, (byte)(includeCount + 1));

                patchBuff:
                bo4.GetBytes(cache.Target.Buffer + 0x8, 8).CopyTo(buffer, 0x8); // crc32
                bo4.GetBytes(cache.Target.Buffer + 0x10, 8).CopyTo(buffer, 0x10); // ScriptName
                cache.hBuffer = bo4.QuickAlloc(buffer.Length); // space
                bo4.SetBytes(cache.hBuffer, buffer); // write to proc
                bo4.SetValue<long>(cache.hTarget + 0x10, cache.hBuffer); // buffer pointer redirect
                cache.Pid = bo4.BaseProcess.Id;
                cache.BufferSize = buffer.Length;
                cache.IsInjected = true;

                try
                {
                    if (cfg.InjectDLL)
                    {
                        string exeFilePath = Assembly.GetExecutingAssembly().Location;
                        var result = bo4.Call<long>(bo4.GetProcAddress(@"kernel32.dll", @"LoadLibraryA"), Path.Combine(Path.GetDirectoryName(exeFilePath), "t8cinternal.dll"));
                        bo4.Refresh();

                        if (result == 0)
                        {
                            return 4;
                        }

                        if (cfg.DllBuiltins)
                        {
                            bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"T8Dll_BuiltinsInit"));
                        }
                        if (cfg.DllLazyLink)
                        {
                            bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"T8Dll_LazyLinkInit"));
                        }
                        if (cfg.DllDetours)
                        {
                            bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"T8Dll_DetoursInit"));
                            if (gsi != null)
                            {
                                // detours
                                if (gsi.Detours.Count > 0)
                                {
                                    bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"RegisterDetours"), gsi.PackDetours(), gsi.Detours.Count, (long)cache.hBuffer, client ? 1 : 0);
                                }
                            } else
                            {
                                // done inside RegisterDetours, useless with gsi
                                bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"RemoveDetours"), client ? 1 : 0);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return 3;
                }
            }
            catch
            {
                return Error("Unknown error while injecting...");
            }
            finally
            {
                bo4.CloseHandle();
            }

            return 0;
        }

        private void FreeT8Script()
        {
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                return;
            }

            FreeT8ScriptCache(bo4, false);
            FreeT8ScriptCache(bo4, true);
        }

        private void FreeT8ScriptClient()
        {
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                return;
            }

            FreeT8ScriptCache(bo4, true);
        }

        private void FreeT8ScriptServer()
        {
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                return;
            }

            FreeT8ScriptCache(bo4, false);
        }

        private void FreeT8ScriptCache(ProcessEx bo4, bool client)
        {
            T8InjectCache cache;

            if (client)
            {
                cache = InjectCacheClient;
            } else
            {
                cache = InjectCache;
            }

            if (!cache.IsInjected)
            {
                return;
            }

            if (bo4.BaseProcess.Id != cache.Pid)
            {
                return;
            }
            bo4.OpenHandle();
            try
            {
                // free allocated space
                ProcessEx.VirtualFreeEx(bo4.Handle, cache.hBuffer, (uint)cache.BufferSize, (int)EnvironmentEx.FreeType.Release);

                // Patch spt struct
                bo4.SetStruct(cache.hTarget, cache.Target);
                bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"RemoveDetours"), client ? 1 : 0);
                cache.IsInjected = false;
            } finally
            {
                bo4.CloseHandle();
            }
}

        private void FreeT7Script()
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

            // Reset hooked detours
            bo3.Call<VOID>(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));

            bo3.CloseHandle();
        }

        private class T8InjectCache
        {
            public T8SPT Surrogate;
            public T8SPT Target;
            public PointerEx hSurrogate;
            public PointerEx hTarget;
            public PointerEx hBuffer;
            public int BufferSize;
            public int Pid;
            public bool IsInjected;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct T7SPT
        {
            public PointerEx llpName;     //00
            public int BuffSize;       //08
            public int Pad;
            public PointerEx lpBuffer;//10
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct T8SPT
        {
            public PointerEx ScriptName;
            public long pad0;
            public PointerEx Buffer;
            public int Size;
            public int Unk0;
        };

        private int cmd_Dump(string[] args, string[] opts)
        {
            return -1;
        }

        #endregion

        private static HashSet<string> HashIdentifierPrefixes = new HashSet<string>() { "script_" };
        public static ulong T8s64Hash(string input)
        {
            input = input.ToLower();

            //if input starts with func_, var_, or hash_, use the provided hash (if possible)
            foreach (string hashprefix in HashIdentifierPrefixes)
            {
                if (input[0] != hashprefix[0] || input.Length <= hashprefix.Length)
                    continue;
                if (!input.StartsWith(hashprefix))
                    continue;
                if (!ulong.TryParse(input.Substring(hashprefix.Length), NumberStyles.HexNumber, default, out ulong result))
                    break;
                return result;
            }

            return 0x7FFFFFFFFFFFFFFF & HashFNV1a(Encoding.ASCII.GetBytes(input));
        }

        uint Com_Hash(string Input, uint IV, uint XORKEY)
        {
            uint hash = IV;

            foreach (char c in Input)
                hash = (char.ToLower(c) ^ hash) * XORKEY;

            hash *= XORKEY;

            return hash;
        }

        public static ulong HashFNV1a(byte[] bytes, ulong fnv64Offset = 14695981039346656037, ulong fnv64Prime = 0x100000001b3)
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
