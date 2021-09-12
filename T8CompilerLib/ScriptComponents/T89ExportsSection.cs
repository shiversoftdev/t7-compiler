using System;
using System.Collections.Generic;
using System.IO;
using T89CompilerLib.OpCodes;

namespace T89CompilerLib.ScriptComponents
{
    public delegate void JumpCommitter(ref byte[] data);

    public sealed class T89ExportsSection : T89ScriptSection
    {
        public const uint EXPORT_ENTRY_SIZE = 24;

        /// <summary>
        /// Script metadata.
        /// </summary>
        internal T89ScriptMetadata ScriptMetadata
        {
            get
            {
                if (__metadata__ == null)
                    __metadata__ = new T89ScriptMetadata(Script);
                return __metadata__;
            }
            set
            {
                __metadata__ = value;
            }
        }

        private T89ScriptMetadata __metadata__;
        public T89ScriptObject Script { get; private set; }
        private T89ExportsSection(T89ScriptObject script) 
        {
            Script = script;
            ScriptExports = new Dictionary<uint, T89ScriptExport>();
        } //prevent public initializer

        internal static T89ExportsSection New(T89ScriptObject script)
        {
            T89ExportsSection exports = new T89ExportsSection(script);
            exports.ScriptExports = new Dictionary<uint, T89ScriptExport>();
            return exports;
        }

        internal Dictionary<uint, T89ScriptExport> ScriptExports;
        private T89ScriptExport FirstExport;

        public override ushort Count()
        {
            return (ushort)ScriptExports.Count;
        }

        public IEnumerable<T89ScriptExport> AllExports()
        {
            foreach (var export in ScriptExports.Values)
                yield return export;
        }

        public IEnumerable<T89ScriptExport> AllExportsSorted()
        {
            int LastLoad = 0;
            int LowestCandidate = 0;
            T89ScriptExport LowestExport = null;
        start:
            LowestCandidate = 0x7FFFFFFF;
            LowestExport = null;
            foreach (var export in ScriptExports.Values)
                if (export.LoadedOffset > LastLoad && export.LoadedOffset < LowestCandidate)
                {
                    LowestCandidate = (int)export.LoadedOffset;
                    LowestExport = export;
                }

            if (LowestExport == null)
                yield break;

            LastLoad = (int)LowestExport.LoadedOffset;
            yield return LowestExport;
            goto start;
        }

        /// <summary>
        /// Serialization was overriden in this class because it makes no sense to serialize the bytecode section when not commiting
        /// </summary>
        /// <returns></returns>
        public override byte[] Serialize()
        {
            throw new InvalidOperationException("Cannot serialize the exports section!");
        }

        public override void Commit(ref byte[] RawData, ref T89ScriptHeader Header)
        {
            int BaseOffset = RawData.Length;

            byte[] NewBuffer = new byte[RawData.Length + HeaderSize()];

            RawData.CopyTo(NewBuffer, 0);
            RawData = NewBuffer;

            FirstExport?.Commit(ref RawData, (uint)BaseOffset, ScriptMetadata);

            //We have to copy again because we need to enforce our section alignment rules
            byte[] FinalBuffer = new byte[(uint)(RawData.Length).AlignValue(0x10)];
            RawData.CopyTo(FinalBuffer, 0);

            RawData = FinalBuffer;

            CommitSize = (uint)(RawData.Length - BaseOffset);

            UpdateHeader(ref Header);
            NextSection?.Commit(ref RawData, ref Header);
        }

        private uint CommitSize;
        public override uint Size()
        {
            return CommitSize;
        }

        private uint HeaderSize()
        {
            return Count() * EXPORT_ENTRY_SIZE;
        }

        public override void UpdateHeader(ref T89ScriptHeader Header)
        {
            Header.ExportsCount = Count();
            Header.ExportTableOffset = GetBaseAddress();
        }

        public static void ReadExports(ref byte[] data, uint lpExportsSection, ushort NumExports, T89ScriptObject script, ref T89ExportsSection Exports)
        {
            T89ScriptMetadata old_meta = Exports?.ScriptMetadata;
            Exports = new T89ExportsSection(script);

            if(old_meta != null)
                Exports.ScriptMetadata = old_meta;

            if (lpExportsSection >= data.Length)
                throw new ArgumentException("Couldn't read the exports section of this gsc because the pointer exceeded the boundaries of the array");

            if (NumExports < 1)
                return;

            uint CurrentPtr = lpExportsSection;

            T89ScriptExport LastExport = null;
            while (NumExports > 0)
            {
                T89ScriptExport export;
                T89ScriptExport.ReadExport(ref data, ref CurrentPtr, ref NumExports, script, out export);

                if (Exports.FirstExport == null)
                    Exports.FirstExport = export;

                Exports.ScriptExports[export.FunctionID] = export;

                export.LinkBack(LastExport);
                LastExport = export;
            }
        }

        /// <summary>
        /// Add a function object to this script
        /// </summary>
        /// <param name="FunctionID">Hashed function ID for export</param>
        /// <param name="NamespaceID">Hashed namespace ID for export</param>
        /// <param name="NumParams">Number of parameters for this function</param>
        /// <returns>A new script export object. If the function already exists, a reference to the existing object.</returns>
        public T89ScriptExport Add(uint FunctionID, uint NamespaceID, byte NumParams)
        {
            if (ScriptExports.ContainsKey(FunctionID))
                return ScriptExports[FunctionID];
            
            T89ScriptExport Previous = FirstExport?.Last();
            T89ScriptExport export = T89ScriptExport.New(Previous, FunctionID, NamespaceID, NamespaceID, NumParams, Script);

            if (FirstExport == null)
                FirstExport = export;

            ScriptExports[FunctionID] = export;
            
            return export;
        }

        /// <summary>
        /// Remove a function object from this script
        /// </summary>
        /// <param name="FunctionID">Hashed ID of the function to remove</param>
        /// <returns></returns>
        public T89ScriptExport Remove(uint FunctionID)
        {
            if (!ScriptExports.ContainsKey(FunctionID))
                return null;

            T89ScriptExport export = ScriptExports[FunctionID];

            ScriptExports.Remove(FunctionID);

            if (export == FirstExport)
            {
                FirstExport = export.NextExport;
            }

            export.Delete();

            return export;
        }

        /// <summary>
        /// Get a function object from this script
        /// </summary>
        /// <param name="FunctionID">Hashed ID of the function to retrieve</param>
        /// <returns></returns>
        public T89ScriptExport Get(uint FunctionID)
        {
            if (ScriptExports.TryGetValue(FunctionID, out T89ScriptExport result))
                return result;

            return null;
        }

        public T89ScriptExport[] GetAll(uint Namespace)
        {
            List<T89ScriptExport> exports = new List<T89ScriptExport>();

            foreach (var export in ScriptExports)
                if (export.Value.Namespace == Namespace)
                    exports.Add(export.Value);

            return exports.ToArray();
        }

        public uint[] GetNSList()
        {
            List<uint> nslist = new List<uint>();
            foreach (var export in ScriptExports)
                if (!nslist.Contains(export.Value.Namespace))
                    nslist.Add(export.Value.Namespace);

            return nslist.ToArray();
        }
    }

    public sealed class T89ScriptExport
    {
        public T89ScriptObject Script { get; private set;}
        private T89ScriptExport(T89ScriptObject script) { Script = script; } //prevent public initializers

        internal static T89ScriptExport New(T89ScriptExport Previous, uint fid, uint ns, uint ns2, byte pcount, T89ScriptObject script)
        {
            T89ScriptExport Export = new T89ScriptExport(script);
            if (Previous != null)
            {
                Export.LastExport = Previous;
                Previous.NextExport = Export;
            }
            Export.FunctionID = fid;
            Export.Namespace = ns;
            Export.Namespace2 = ns2;
            Export.NumParams = pcount;
            Export.Flags = 0;
            Export.Locals = new T89OP_SafeCreateLocalVariables();
            Export.OpCodes.Add(Export.Locals);
            Export.FriendlyName = "func_" + fid.ToString("X");
            return Export;
        }

        public T89ScriptExport LastExport { get; private set; }
        public T89ScriptExport NextExport { get; private set; }

        /// <summary>
        /// The friendly name of this function, for use in compilation reporting.
        /// </summary>
        public string FriendlyName;
        public uint CRC32 { get; private set; }
        public uint LoadedOffset { get; private set; }
        public uint FunctionID { get; private set; }
        public uint Namespace { get; private set; }
        public uint Namespace2 { get; private set; }
        public byte NumParams { get; private set; }
        public byte Flags { get; set; }

        /// <summary>
        /// This is used when we want to perform quick removes/adds from the table
        /// </summary>
        private readonly HashSet<T89OpCode> OpCodes = new HashSet<T89OpCode>();

        /// <summary>
        /// This is used when we want to walk the opcodes through a linkedlist. Should always be either OP_CheckClearParams or OP_SafeCreateLocalVariables
        /// </summary>
        public T89OP_SafeCreateLocalVariables Locals { get; private set; }

        /// <summary>
        /// Dictionary of builtin objects
        /// </summary>
        private static readonly Dictionary<string, ScriptOpCode> BuiltinObjects = new Dictionary<string, ScriptOpCode>()
        {
            { "undefined_obj", ScriptOpCode.GetUndefined },
            { "true_obj", ScriptOpCode.GetInteger },
            { "false_obj", ScriptOpCode.GetZero },
            { "self_obj", ScriptOpCode.GetSelf },
            { "self_ref", ScriptOpCode.GetSelfObject }
        };

        private static readonly HashSet<string> GlobalObjectTable = new HashSet<string>()
        {
            "world",
            "mission",
            "level",
            "game",
            "anim",
            "classes",
            "structs"
        };

        /// <summary>
        /// Dictionary of the builtin functions
        /// </summary>
        private static readonly Dictionary<string, ScriptOpCode> BuiltinFunctions = new Dictionary<string, ScriptOpCode>()
        {
            { "realwait", ScriptOpCode.RealWait },
            { "isdefined", ScriptOpCode.IsDefined },
            { "vectorscale", ScriptOpCode.VectorScale },
            { "gettime", ScriptOpCode.GetTime },
            { "waitframe", ScriptOpCode.WaitRealTime },
            { "waittillframeend", ScriptOpCode.WaitTillFrameEnd },
        };

        /// <summary>
        /// Builtin notifiers
        /// </summary>
        private static readonly Dictionary<string, ScriptOpCode> Notifiers = new Dictionary<string, ScriptOpCode>()
        {
            { "notify", ScriptOpCode.Notify},
            { "endon", ScriptOpCode.EndOn },
            { "waittill_match", ScriptOpCode.WaitTillMatch },
            { "waittill", ScriptOpCode.WaitTill },
            { "endon_callback", ScriptOpCode.EndOnCallback },
            { "waittill_timeout", ScriptOpCode.WaittillTimeout }
        };

        private static readonly Dictionary<string, ScriptOpCode> MathTokens = new Dictionary<string, ScriptOpCode>()
        {
            { "+", ScriptOpCode.Plus },
            { "-", ScriptOpCode.Minus },
            { "*", ScriptOpCode.Multiply },
            { "/", ScriptOpCode.Divide },
            { "%", ScriptOpCode.Modulus },
            { "&", ScriptOpCode.Bit_And },
            { "|", ScriptOpCode.Bit_Or },
            { "^", ScriptOpCode.Bit_Xor },
            { "<<", ScriptOpCode.ShiftLeft },
            { ">>", ScriptOpCode.ShiftRight },
        };

        private static readonly Dictionary<string, ScriptOpCode> CompareOps = new Dictionary<string, ScriptOpCode>()
        {
            { ">", ScriptOpCode.GreaterThan },
            { ">=", ScriptOpCode.GreaterThanOrEqualTo },
            { "<", ScriptOpCode.LessThan },
            { "<=", ScriptOpCode.LessThanOrEqualTo },
            { "==", ScriptOpCode.Equal },
            { "!=", ScriptOpCode.NotEqual },
            { "===", ScriptOpCode.SuperEqual },
            { "!==", ScriptOpCode.SuperNotEqual },
        };

        private readonly Stack<string> ForeachKeys = new Stack<string>();

        private readonly List<string> SwitchKeys = new List<string>();

        //utilized strictly for decompiling. May eventually export to the decompiler.
        public List<string> OperandStack = new List<string>();
        public string StackVariableRef = "";
        public string StackObject = "";

        /// <summary>
        /// Stack of LCF for this function
        /// </summary>
        private readonly Dictionary<int, List<T89OP_Jump>> LCFStack = new Dictionary<int, List<T89OP_Jump>>();

        /// <summary>
        /// Context for the lcf stack
        /// </summary>
        private int LCFContext;

        public static void ReadExport(ref byte[] data, ref uint lpExportPtr, ref ushort NumExports, T89ScriptObject script, out T89ScriptExport export)
        {
            export = new T89ScriptExport(script);
            export.Locals = new T89OP_SafeCreateLocalVariables();

            if (lpExportPtr >= data.Length)
                throw new ArgumentException("Couldn't load the exports of this gsc because an export points outside of the boundaries of the data given.");

            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            reader.BaseStream.Position = lpExportPtr;

            export.CRC32 = reader.ReadUInt32();

            export.LoadedOffset = reader.ReadUInt32();

            export.FunctionID = reader.ReadUInt32();
            export.Namespace = reader.ReadUInt32();
            export.Namespace2 = reader.ReadUInt32();
            export.NumParams = reader.ReadByte();
            export.Flags = reader.ReadByte();
            export.FriendlyName = "func_" + export.FunctionID.ToString("X");
            lpExportPtr = (uint)reader.BaseStream.Position + 2;

            NumExports--;

            reader.Dispose();

            export.LoadBytecode(ref data, export.LoadedOffset, script);
        }

        private void LoadBytecode(ref byte[] raw, uint lpByteCodeStart, T89ScriptObject script)
        {
            return;            
        }

        private T89OpCode LoadOperation(ScriptOpCode code, BinaryReader reader, T89ScriptObject script)
        {
            return null;
        }

        private static bool IsTerminalOp(ScriptOpCode code)
        {
            return code == ScriptOpCode.Invalid || code == ScriptOpCode.End || code == ScriptOpCode.Return;
        }

        private static bool IsPrefixOp(ScriptOpCode code)
        {
            return code == ScriptOpCode.SafeCreateLocalVariables || code == ScriptOpCode.CheckClearParams;
        }

        private bool ScriptOpAt(long Position)
        {
            foreach (var op in OpCodes)
                if (op.CommitAddress == Position)
                    return true;
            return false;
        }

        private void AddLoadedOp(T89OpCode code, T89OpCode target, uint lpCommitAddress, ushort LoadValue)
        {
            __addop_internal(code, target);
            code.CommitAddress = lpCommitAddress;
            code.LoadedValue = LoadValue;
        }

        public void Commit(ref byte[] data, uint NextExportPtr, T89ScriptMetadata EmissionTable)
        {
            OptimizeExport();
            List<byte> OpCodeData = new List<byte>();

            int ByteCodeAddress = data.Length.AlignValue(0x10); //It seems like in retail all gscs are actually qword aligned. If this doesnt work we need to literally do '8' aligned

            //not only is it 8 aligned, it seems like they always precede it with at least 8 nulls. I believe this is to inject the function header into the bytecode.
            ByteCodeAddress += 0x8;

            uint baseaddress = (uint)ByteCodeAddress;

            T89OpCode currOp = Locals;

            while (currOp != null)
            {
                currOp?.Commit(ref OpCodeData, ref baseaddress, EmissionTable);
                currOp = currOp.NextOpCode;
            }

            byte[] NewBuffer = new byte[ByteCodeAddress + OpCodeData.Count];

            data.CopyTo(NewBuffer, 0);
            OpCodeData.CopyTo(NewBuffer, ByteCodeAddress);

            data = NewBuffer;

            CommitJumps?.Invoke(ref data);

            BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
            writer.BaseStream.Position = NextExportPtr;

            writer.Write((int)0);
            writer.Write(ByteCodeAddress);
            writer.Write(FunctionID);
            writer.Write(Namespace);
            writer.Write(Namespace);
            writer.Write(NumParams);
            writer.Write((byte)Flags);
            writer.Write((ushort)0x0);
            writer.Dispose();

            NextExportPtr += T89ExportsSection.EXPORT_ENTRY_SIZE;

            NextExport?.Commit(ref data, NextExportPtr, EmissionTable);
        }

        public void LinkBack(T89ScriptExport Previous)
        {
            if (Previous != null)
                Previous.NextExport = this;
            LastExport = Previous;
        }

        public T89ScriptExport Last()
        {
            return NextExport?.Last() ?? this;
        }

        internal void Delete()
        {
            if (LastExport != null)
                LastExport.NextExport = NextExport;

            if (NextExport != null)
                NextExport.LastExport = LastExport;
        }

        /// <summary>
        /// Emit an operation to this instance
        /// </summary>
        /// <param name="OpData"></param>
        /// <param name="arguments"></param>
        public T89OpCode AddOp(ScriptOpCode OpCode)
        {
            //During development, this should stay a massive switch so the notimplementedexception is triggered
            switch (OpCode)
            {
                case ScriptOpCode.CastBool:
                case ScriptOpCode.EvalFieldVariableOnStack:
                case ScriptOpCode.EvalFieldVariableOnStackRef:
                case ScriptOpCode.CastVariableName:
                case ScriptOpCode.CreateStruct:
                case ScriptOpCode.AddToStruct:
                case ScriptOpCode.AddToArray:
                case ScriptOpCode.VoidCodePos:
                case ScriptOpCode.FirstArrayKey:
                case ScriptOpCode.NextArrayKey:
                case ScriptOpCode.IsDefined:
                case ScriptOpCode.EvalArrayRef:
                case ScriptOpCode.EvalArray:
                case ScriptOpCode.GetEmptyArray:
                case ScriptOpCode.BoolNot:
                case ScriptOpCode.CastFieldObject:
                case ScriptOpCode.Vector:
                case ScriptOpCode.DecTop:
                case ScriptOpCode.EndOn:
                case ScriptOpCode.Notify:
                case ScriptOpCode.ClearParams:
                case ScriptOpCode.WaitTill:
                case ScriptOpCode.PreScriptCall:
                case ScriptOpCode.SetVariableField:
                case ScriptOpCode.Dec:
                case ScriptOpCode.Inc:
                case ScriptOpCode.SizeOf:
                case ScriptOpCode.WaitTillFrameEnd:
                case ScriptOpCode.Return:
                case ScriptOpCode.End:
                case ScriptOpCode.Wait:
                case ScriptOpCode.WaitFrame:
                    return __addop_internal(new T89OpCode(OpCode));

                default:
                    throw new NotImplementedException($"AddOp tried to add operation '{OpCode.ToString()}', but this operation is not handled!");
            }
        }

        /// <summary>
        /// Remove an operation from this script
        /// </summary>
        /// <param name="Operation">Operation to remove</param>
        /// <param name="breakchain">Remove all subsequent operations</param>
        /// <returns></returns>
        public T89OpCode RemoveOp(T89OpCode Operation)
        {
            if (Operation == null)
                return null;

            if (Operation == Locals) //cant remove locals
                throw new ArgumentException("Cannot remove the local variable creator from an export");

            OpCodes.Remove(Operation);

            Operation.Unlink();

            return Operation;
        }

        private T89OpCode __addop_internal(T89OpCode code, T89OpCode target = null)
        {
            OpCodes.Add(code);

            if (target == null)
                target = Locals.GetEndOfChain();

            target?.Append(code);
            return code;
        }

        private void OptimizeExport()
        {
            return;
            
        }

        /// <summary>
        /// Add any of the opcodes that get a numeric value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T89OpCode AddGetNumber(object value)
        {
            return __addop_internal(new T89OP_GetNumericValue(value));
        }

        /// <summary>
        /// Add a return/end node that dynamically decides its state based on its linked partner
        /// </summary>
        /// <returns></returns>
        public T89OpCode AddReturn()
        {
            return __addop_internal(new T89OP_Return());
        }

        /// <summary>
        /// Add a getstring reference
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public T89OpCode AddGetString(T89StringTableEntry str, bool isIstring = false)
        {
            return __addop_internal(new T89OP_GetString(isIstring ? ScriptOpCode.GetIString : ScriptOpCode.GetString, str));
        }

        /// <summary>
        /// Try to emit a local variable. Will throw an ArgumentException if the local cant be resolved
        /// </summary>
        /// <param name="Identifier">Lowercase version of the identifier. If not, we will run into ref issues</param>
        /// <param name="_ref"></param>
        /// <returns></returns>
        public T89OpCode AddEvalLocal(string identifier, uint hashformat, bool _ref = false, bool HasWaittillContext = false)
        {
            T89OpCode code = null;

            if (TryCreateBuiltin(identifier, _ref, ref code))
                return __addop_internal(code);
            try
            {
                if (_ref)   code = new T89OP_GetLocal(Locals, hashformat, Script.VM == VMREVISIONS.VM_36 ? ScriptOpCode.EvalLocalVariableRefCached : ScriptOpCode.EvalLocalVariableRefCached2);
                else        code = new T89OP_GetLocal(Locals, hashformat, ScriptOpCode.EvalLocalVariableCached);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Tried to access unknown variable '{identifier}' in function '{FriendlyName}'");
            }
            return __addop_internal(code);
        }

        public T89OpCode AddAssignLocal(string identifier, uint hashformat)
        {
            return __addop_internal(new T89OP_SetLocal(Locals, hashformat));
        }

        public T89OpCode AddEvalLocalDefined(string identifier, uint hashformat)
        {
            return __addop_internal(new T89OP_GetLocal(Locals, hashformat, ScriptOpCode.EvalLocalVariableDefined));
        }

        public T89OpCode AddAssignArrayKey(string identifier, uint hashformat, bool first)
        {
            return __addop_internal(new T89OP_GetLocal(Locals, hashformat, first ? ScriptOpCode.FirstArrayKeyCached : ScriptOpCode.SetNextArrayKeyCached));
        }

        /// <summary>
        /// Try to create a builtin emission
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="_ref"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private bool TryCreateBuiltin(string identifier, bool _ref, ref T89OpCode code)
        {
            if (BuiltinObjects.TryGetValue(identifier + (_ref ? "_ref" : "_obj"), out ScriptOpCode op))
            {
                switch (op)
                {
                    //true
                    case ScriptOpCode.GetInteger:
                        code = new T89OP_GetNumericValue(1);
                        return code != null;

                    default:
                        code = new T89OpCode(op);
                        return code != null;
                }
            }

            if(GlobalObjectTable.Contains(identifier))
            {
                return (code = new T89OP_GetGlobal(Script.Globals.AddGlobal(Script.T8Hash(identifier)), _ref)) != null;
            }

            return false;
        }

        public T89OpCode AddGlobalObject(string identifier, bool isRef)
        {
            return __addop_internal(new T89OP_GetGlobal(Script.Globals.AddGlobal(Script.T8Hash(identifier)), isRef));
        }

        /// <summary>
        /// Try to add a builtin object reference
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="_ref"></param>
        /// <returns></returns>
        public T89OpCode TryAddBuiltIn(string identifier, bool _ref)
        {
            T89OpCode code = null;

            if (TryCreateBuiltin(identifier.ToLower(), _ref, ref code))
                return __addop_internal(code);

            return null;
        }

        /// <summary>
        /// Try to add a builtin call
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public T89OpCode TryAddBuiltInCall(string identifier)
        {
            if (BuiltinFunctions.TryGetValue(identifier.ToLower(), out ScriptOpCode opcode))
                return __addop_internal(new T89OpCode(opcode));

            return null;
        }

        /// <summary>
        /// Query the local builtin table for an identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static bool IsBuiltinCall(string identifier)
        {
            return BuiltinFunctions.ContainsKey(identifier.ToLower());
        }

        /// <summary>
        /// Query the local notifier table for an identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static bool IsNotifier(string identifier)
        {
            return Notifiers.ContainsKey(identifier.ToLower());
        }

        /// <summary>
        /// Query both builtin defs for an identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static bool IsBuiltinMethod(string identifier)
        {
            return IsBuiltinCall(identifier) || IsNotifier(identifier);
        }

        /// <summary>
        /// Add a call based on a pointer
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="NumParams"></param>
        /// <returns></returns>
        public T89OpCode AddCallPtr(uint Context, byte NumParams)
        {
            return __addop_internal(new T89OP_CallPtr(Context, NumParams));
        }

        /// <summary>
        /// Add a call based on a reference or namespace
        /// </summary>
        /// <param name="import"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public T89OpCode AddCall(T89Import import, uint context)
        {
            return __addop_internal(new T89OP_Call(import, context));
        }

        /// <summary>
        /// Add a reference to an imported function
        /// </summary>
        /// <param name="import"></param>
        /// <returns></returns>
        public T89OpCode AddFunctionPtr(T89Import import)
        {
            return __addop_internal(new T89OP_GetFuncPtr(import, ScriptOpCode.GetAPIFunction));
        }

        /// <summary>
        /// Add a field variable ref to the current function
        /// </summary>
        /// <param name="FunctionHash"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public T89OpCode AddFieldVariable(uint FunctionHash, uint Context)
        {
            return __addop_internal(new T89OP_EvalFieldVariable(FunctionHash, Context));
        }

        /// <summary>
        /// Add a stack variable ref to the current function
        /// </summary>
        /// <param name="FunctionHash"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public T89OpCode AddStackVariable(uint Context)
        {
            AddOp(ScriptOpCode.CastVariableName);
            return AddOp((Context & (uint)ScriptContext.IsRef) > 0 ? ScriptOpCode.EvalFieldVariableOnStackRef : ScriptOpCode.EvalFieldVariableOnStack);
        }

        /// <summary>
        /// Handler for jumps to commit after the export size finalizes
        /// </summary>
        private JumpCommitter CommitJumps = (ref byte[] d) => {};

        /// <summary>
        /// Add a jump to this function.
        /// </summary>
        /// <param name="OpType"></param>
        /// <returns></returns>
        public T89OP_Jump AddJump(ScriptOpCode OpType)
        {
            T89OP_Jump jmp = new T89OP_Jump(OpType);

            CommitJumps += jmp.CommitJump; //bind the event

            /*if (OpType == ScriptOpCode.JumpOnFalse || OpType == ScriptOpCode.JumpOnTrue)
                __addop_internal(new T89OpCode(ScriptOpCode.CastBool));*/

            return (T89OP_Jump) __addop_internal(jmp);
        }

        /// <summary>
        /// Push loop control flow (continue, break)
        /// </summary>
        /// <param name="RefHead">Should we refer to the head of the loop, or the end.</param>
        /// <returns></returns>
        public T89OP_Jump PushLCF(bool RefHead, int offset = 0)
        {
            T89OP_Jump jmp = AddJump(ScriptOpCode.Jump);
            jmp.RefHead = RefHead;
            int RealContext = LCFContext - offset;

            if (!LCFStack.ContainsKey(RealContext))
                LCFStack[RealContext] = new List<T89OP_Jump>();

            LCFStack[RealContext].Insert(0, jmp);

            return jmp;
        }

        /// <summary>
        /// Pop a loop control flow from the stack
        /// </summary>
        /// <param name="jmp"></param>
        /// <returns></returns>
        public bool TryPopLCF(out T89OP_Jump jmp)
        {
            jmp = null;

            if(!LCFStack.ContainsKey(LCFContext) || LCFStack[LCFContext] == null)
                return false;

            if (LCFStack[LCFContext].Count < 1)
                return false;

            jmp = LCFStack[LCFContext][0];
            LCFStack[LCFContext].RemoveAt(0);

            return true;
        }

        /// <summary>
        /// Increment the LCF context
        /// </summary>
        public void IncLCFContext()
        {
            LCFContext++;
        }

        /// <summary>
        /// Decrement the LCF context
        /// </summary>
        public void DecLCFContext()
        {
            LCFContext--;
        }

        /// <summary>
        /// Add a math operator to the stack
        /// </summary>
        /// <param name="Token"></param>
        public T89OpCode AddMathToken(string token)
        {
            if (MathTokens.TryGetValue(token, out ScriptOpCode code))
                return __addop_internal(new T89OpCode(code));

            throw new NotImplementedException($"Math operator '{token}' has not been handled.");
        }

        /// <summary>
        /// Add a compare operator to the stack
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public T89OpCode AddCompareOp(string token)
        {
            if (CompareOps.TryGetValue(token, out ScriptOpCode code))
                return __addop_internal(new T89OpCode(code));
            throw new NotImplementedException($"CompareOp '{token}' has not been handled.");
        }

        /// <summary>
        /// Push a foreach kvp into the local function's array
        /// </summary>
        /// <param name="keypair"></param>
        public void PushFEKeys(params string[] keys)
        {
            foreach(string key in keys)
                ForeachKeys.Push(key);
        }

        /// <summary>
        /// Try to pop a foreach kvp from the local function's array
        /// </summary>
        /// <param name="keypair"></param>
        /// <returns></returns>
        public bool TryPopFEKeys(out string[] keypair, int count)
        {
            if(ForeachKeys.Count < 1)
            {
                keypair = null;
                return false;
            }
            keypair = new string[count];
            for (int i = 0; i < count; i++) keypair[i] = ForeachKeys.Pop();
            return true;
        }

        /// <summary>
        /// Push a switch key onto the local stack
        /// </summary>
        /// <param name="key"></param>
        public void PushSwitchKey(string key)
        {
            SwitchKeys.Add(key);
        }

        /// <summary>
        /// Try to pop a switch key from the local stack
        /// </summary>
        /// <param name="keypair"></param>
        /// <returns></returns>
        public bool TryPopSwitchKey(out string key)
        {
            if (SwitchKeys.Count < 1)
            {
                key = null;
                return false;
            }

            key = SwitchKeys[0];
            SwitchKeys.RemoveAt(0);

            return true;
        }

        /// <summary>
        /// Add gethash to the current function
        /// </summary>
        /// <param name="Hash"></param>
        /// <returns></returns>
        public T89OpCode AddGetHash(ulong Hash)
        {
            return __addop_internal(new T89OP_GetHash(Hash));
        }

        /// <summary>
        /// Add a notification typed opcode
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="NumParams"></param>
        /// <returns></returns>
        public T89OpCode AddNotification(string notification, byte NumParams)
        {
            return __addop_internal(new T89OP_Notification(Notifiers[notification.ToLower()], NumParams));
        }

        public override string ToString()
        {
            return $"{FunctionID:X8}[{NumParams}]@{LoadedOffset:X8}";
        }
    }
}
