using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using T7CompilerLib.OpCodes;

//if ( f_ns != 0x33B293FD && f_ns != 0x2E3CA4C4 && !(script_import->flags & NeedsResolver) )
//33x is ""
//2ex is sys
//Hardcoded namespaces that may not reference their own fuctions

namespace T7CompilerLib.ScriptComponents
{
    public delegate void JumpCommitter(ref byte[] data);

    public sealed class T7ExportsSection : T7ScriptSection
    {
        private T7ScriptObject Script;
        public const uint EXPORT_ENTRY_SIZE = 20;
        private EndianType Endianess;
        /// <summary>
        /// Script metadata.
        /// </summary>
        internal T7ScriptMetadata ScriptMetadata
        {
            get
            {
                if (__metadata__ == null)
                    __metadata__ = new T7ScriptMetadata(Script);
                return __metadata__;
            }
            set
            {
                __metadata__ = value;
            }
        }

        private T7ScriptMetadata __metadata__;

        private T7ExportsSection(bool littleEndian, T7ScriptObject obj) 
        {
            Script = obj;
            Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian;
            ScriptExports = new Dictionary<uint, T7ScriptExport>();
        } //prevent public initializer

        internal static T7ExportsSection New(bool littleEndian, T7ScriptObject obj)
        {
            T7ExportsSection exports = new T7ExportsSection(littleEndian, obj);
            exports.ScriptExports = new Dictionary<uint, T7ScriptExport>();
            return exports;
        }

        internal Dictionary<uint, T7ScriptExport> ScriptExports;
        private T7ScriptExport FirstExport;

        public override ushort Count()
        {
            return (ushort)ScriptExports.Count;
        }

        public IEnumerable<T7ScriptExport> AllExports()
        {
            foreach (var export in ScriptExports.Values)
                yield return export;
        }

        public IEnumerable<T7ScriptExport> AllExportsSorted()
        {
            int LastLoad = 0;
            int LowestCandidate = 0;
            T7ScriptExport LowestExport = null;
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

        public override void Commit(ref byte[] RawData, ref T7ScriptHeader Header)
        {
            int BaseOffset = RawData.Length;

            byte[] NewBuffer = new byte[RawData.Length + HeaderSize()];

            RawData.CopyTo(NewBuffer, 0);
            RawData = NewBuffer;

            FirstExport?.Commit(ref RawData, (uint)BaseOffset, Header, ScriptMetadata);

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

        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
            Header.ExportsCount = Count();
            Header.ExportTableOffset = GetBaseAddress();
            Header.ByteCodeOffset = GetBaseAddress() + HeaderSize();
            Header.ByteCodeSize = Size() - HeaderSize();
        }

        public static void ReadExports(ref byte[] data, bool littleEndian, uint lpExportsSection, ushort NumExports, uint EndOfBytecode, ref T7ExportsSection Exports, T7ScriptObject obj)
        {
            T7ScriptMetadata old_meta = Exports?.ScriptMetadata;
            Exports = new T7ExportsSection(littleEndian, obj);

            if(old_meta != null)
                Exports.ScriptMetadata = old_meta;

            if (lpExportsSection >= data.Length)
                throw new ArgumentException("Couldn't read the exports section of this gsc because the pointer exceeded the boundaries of the array");

            if (NumExports < 1)
                return;

            uint CurrentPtr = lpExportsSection;

            T7ScriptExport LastExport = null;
            while (NumExports > 0)
            {
                T7ScriptExport export;
                T7ScriptExport.ReadExport(ref data, littleEndian, ref CurrentPtr, ref NumExports, EndOfBytecode, out export);

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
        public T7ScriptExport Add(uint FunctionID, uint NamespaceID, byte NumParams)
        {
            if (ScriptExports.ContainsKey(FunctionID))
                return ScriptExports[FunctionID];
            
            T7ScriptExport Previous = FirstExport?.Last();
            T7ScriptExport export = T7ScriptExport.New(Previous, Endianess == EndianType.LittleEndian, FunctionID, NamespaceID, NumParams);

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
        public T7ScriptExport Remove(uint FunctionID)
        {
            if (!ScriptExports.ContainsKey(FunctionID))
                return null;

            T7ScriptExport export = ScriptExports[FunctionID];

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
        public T7ScriptExport Get(uint FunctionID)
        {
            if (ScriptExports.TryGetValue(FunctionID, out T7ScriptExport result))
                return result;

            return null;
        }
    }

    public sealed class T7ScriptExport
    {
        private EndianType Endianess;
        private T7ScriptExport(bool littleEndian) { Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian; } //prevent public initializers

        internal static T7ScriptExport New(T7ScriptExport Previous, bool littleEndian, uint fid, uint ns, byte pcount)
        {
            T7ScriptExport Export = new T7ScriptExport(littleEndian);
            if (Previous != null)
            {
                Export.LastExport = Previous;
                Previous.NextExport = Export;
            }
            Export.FunctionID = fid;
            Export.Namespace = ns;
            Export.NumParams = pcount;
            Export.Flags = 0;
            Export.Locals = new T7OP_SafeCreateLocalVariables(Export.Endianess);
            Export.OpCodes.Add(Export.Locals);
            Export.FriendlyName = "func_" + fid.ToString("X");
            return Export;
        }

        public T7ScriptExport LastExport { get; private set; }
        public T7ScriptExport NextExport { get; private set; }

        /// <summary>
        /// The friendly name of this function, for use in compilation reporting.
        /// </summary>
        public string FriendlyName;
        public uint CRC32 { get; private set; }
        public uint FunctionID { get; private set; }
        public uint Namespace { get; private set; }
        public byte NumParams { get; private set; }
        public byte Flags { get; set; }

        public uint LoadedOffset;
        internal uint LoadedSize;

        /// <summary>
        /// This is used when we want to perform quick removes/adds from the table
        /// </summary>
        private readonly HashSet<T7OpCode> OpCodes = new HashSet<T7OpCode>();

        /// <summary>
        /// This is used when we want to walk the opcodes through a linkedlist. Should always be either OP_CheckClearParams or OP_SafeCreateLocalVariables
        /// </summary>
        public T7OP_SafeCreateLocalVariables Locals { get; private set; }

        /// <summary>
        /// Dictionary of builtin objects
        /// </summary>
        private static readonly Dictionary<string, ScriptOpCode> BuiltinObjects = new Dictionary<string, ScriptOpCode>()
        {
            { "undefined_obj", ScriptOpCode.GetUndefined },
            { "true_obj", ScriptOpCode.GetInteger },
            { "false_obj", ScriptOpCode.GetZero },
            { "self_obj", ScriptOpCode.GetSelf },
            { "self_ref", ScriptOpCode.GetSelfObject },
            { "level_obj", ScriptOpCode.GetLevel },
            { "level_ref", ScriptOpCode.GetLevelObject },
            { "game_obj", ScriptOpCode.GetGame },
            { "game_ref", ScriptOpCode.GetGameRef },
            { "anim_ref", ScriptOpCode.GetAnimObject },
            { "world_ref", ScriptOpCode.GetWorldObject },
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
            { "firstarraykey", ScriptOpCode.FirstArrayKey },
            { "nextarraykey", ScriptOpCode.NextArrayKey },
            { "getfirstarraykey", ScriptOpCode.FirstArrayKey },
            { "getnextarraykey", ScriptOpCode.NextArrayKey },
            { "waitrealtime", ScriptOpCode.WaitRealTime }
        };

        /// <summary>
        /// Builtin notifiers
        /// </summary>
        private static readonly Dictionary<string, ScriptOpCode> Notifiers = new Dictionary<string, ScriptOpCode>()
        {
            { "notify", ScriptOpCode.RealWait },
            { "endon", ScriptOpCode.RealWait },
            { "waittillmatch", ScriptOpCode.RealWait },
            { "waittill", ScriptOpCode.RealWait }
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
            { "!===", ScriptOpCode.SuperNotEqual },
        };

        private readonly List<KeyValuePair<string, string>> ForeachKeys = new List<KeyValuePair<string, string>>();

        private readonly List<string> SwitchKeys = new List<string>();

        /// <summary>
        /// Stack of LCF for this function
        /// </summary>
        private readonly Dictionary<int, List<T7OP_Jump>> LCFStack = new Dictionary<int, List<T7OP_Jump>>();

        /// <summary>
        /// Context for the lcf stack
        /// </summary>
        private int LCFContext;

        public static void ReadExport(ref byte[] data, bool littleEndian, ref uint lpExportPtr, ref ushort NumExports, uint EndOfBytecode, out T7ScriptExport export)
        {
            export = new T7ScriptExport(littleEndian);

            if (lpExportPtr >= data.Length)
                throw new ArgumentException("Couldn't load the exports of this gsc because an export points outside of the boundaries of the data given.");

            EndianReader reader = new EndianReader(new MemoryStream(data), export.Endianess);
            reader.BaseStream.Position = lpExportPtr;

            export.CRC32 = reader.ReadUInt32();

            export.LoadedOffset = reader.ReadUInt32();

            export.FunctionID = reader.ReadUInt32();
            export.Namespace = reader.ReadUInt32();
            export.NumParams = reader.ReadByte();
            export.Flags = reader.ReadByte();
            export.FriendlyName = "func_" + export.FunctionID.ToString("X");
            lpExportPtr = (uint)reader.BaseStream.Position + 2;

            NumExports--;

            if (NumExports > 0)
            {
                reader.ReadUInt32();
                EndOfBytecode = reader.ReadUInt32();
            }

            reader.Dispose();
            if (export.LoadedOffset >= data.Length)
            {
                return; //Need to log: An export's bytecode wasn't loaded because the bytecode points outside of the boundaries of the data given.
            }

            export.LoadedSize = EndOfBytecode - export.LoadedOffset;

            export.LoadBytecode(ref data, export.LoadedOffset, EndOfBytecode);
        }

        private void LoadBytecode(ref byte[] raw, uint lpByteCodeStart, uint lpByteCodeEnd)
        {
            //set FirstOpCode
        }

        public void Commit(ref byte[] data, uint NextExportPtr, T7ScriptHeader header, T7ScriptMetadata EmissionTable)
        {
            List<byte> OpCodeData = new List<byte>();

            int ByteCodeAddress = data.Length.AlignValue(0x10); //It seems like in retail all gscs are actually qword aligned. If this doesnt work we need to literally do '8' aligned

            //not only is it 8 aligned, it seems like they always precede it with at least 8 nulls. I believe this is to inject the function header into the bytecode.
            ByteCodeAddress += 0x8;

            uint baseaddress = (uint)ByteCodeAddress;

            T7OpCode currOp = Locals;

            while(currOp != null)
            {
                currOp?.Commit(ref OpCodeData, ref baseaddress, header, EmissionTable);
                currOp = currOp.NextOpCode;
            }

            byte[] NewBuffer = new byte[ByteCodeAddress + OpCodeData.Count];

            data.CopyTo(NewBuffer, 0);
            OpCodeData.CopyTo(NewBuffer, ByteCodeAddress);

            data = NewBuffer;

            CommitJumps?.Invoke(ref data);

            EndianWriter writer = new EndianWriter(new MemoryStream(data), Endianess);
            writer.BaseStream.Position = NextExportPtr;

            CRC32 crc32 = new CRC32();

            for(int i = ByteCodeAddress; i < ByteCodeAddress + OpCodeData.Count; i++)
            {
                crc32.Update(data[i]);
            }

            CRC32 = crc32.Value;

            writer.Write((int)-1);
            writer.Write(ByteCodeAddress);
            writer.Write(FunctionID);
            writer.Write(Namespace);
            writer.Write(NumParams);
            writer.Write(Flags);
            writer.Write((ushort)0x0);
            writer.Dispose();

            NextExportPtr += T7ExportsSection.EXPORT_ENTRY_SIZE;

            NextExport?.Commit(ref data, NextExportPtr, header, EmissionTable);
        }

        public void LinkBack(T7ScriptExport Previous)
        {
            if (Previous != null)
                Previous.NextExport = this;
            LastExport = Previous;
        }

        public T7ScriptExport Last()
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
        public T7OpCode AddOp(ScriptOpCode OpCode)
        {
            //During development, this should stay a massive switch so the notimplementedexception is triggered
            switch (OpCode)
            {
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
                    return __addop_internal(new T7OpCode(OpCode, Endianess));

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
        public T7OpCode RemoveOp(T7OpCode Operation)
        {
            if (Operation == null)
                return null;

            if (Operation == Locals) //cant remove locals
                throw new ArgumentException("Cannot remove the local variable creator from an export");

            OpCodes.Remove(Operation);

            Operation.Unlink();

            return Operation;
        }

        private T7OpCode __addop_internal(T7OpCode code)
        {
            OpCodes.Add(code);
            Locals.GetEndOfChain().Append(code);
            return code;
        }

        /// <summary>
        /// Add any of the opcodes that get a numeric value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T7OpCode AddGetNumber(object value)
        {
            return __addop_internal(new T7OP_GetNumericValue(value, Endianess));
        }

        /// <summary>
        /// Add a return/end node that dynamically decides its state based on its linked partner
        /// </summary>
        /// <returns></returns>
        public T7OpCode AddReturn()
        {
            return __addop_internal(new T7OP_Return(Endianess));
        }

        /// <summary>
        /// Add a getstring reference
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public T7OpCode AddGetString(T7StringTableEntry str, bool isIstring = false)
        {
            return __addop_internal(new T7OP_GetString(isIstring ? ScriptOpCode.GetIString : ScriptOpCode.GetString, str, Endianess));
        }

        /// <summary>
        /// Try to emit a local variable. Will throw an ArgumentException if the local cant be resolved
        /// </summary>
        /// <param name="Identifier">Lowercase version of the identifier. If not, we will run into ref issues</param>
        /// <param name="_ref"></param>
        /// <returns></returns>
        public T7OpCode AddEvalLocal(string identifier, uint hashformat, bool _ref = false, bool HasWaittillContext = false)
        {
            T7OpCode code = null;

            if (TryCreateBuiltin(identifier, _ref, ref code))
                return __addop_internal(code);

            try
            {
                if (HasWaittillContext)
                    code = new T7OP_GetLocal(Locals, hashformat, ScriptOpCode.SetWaittillVariableFieldCached, Endianess);
                else if (_ref)
                    code = new T7OP_GetLocal(Locals, hashformat, ScriptOpCode.EvalLocalVariableRefCached, Endianess);
                else
                    code = new T7OP_GetLocal(Locals, hashformat, ScriptOpCode.EvalLocalVariableCached, Endianess);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Tried to access unknown variable '{identifier}' in function '{FriendlyName}'");
            }

            return __addop_internal(code);
        }

        /// <summary>
        /// Try to create a builtin emission
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="_ref"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private bool TryCreateBuiltin(string identifier, bool _ref, ref T7OpCode code)
        {
            //Appending 'obj' is necessary because if we dont tag the variable based on ref status, an injection will exist with variable names that will cause a crash.
            //A fix to this that involves no tagging is 2 separate dictionaries
            if (BuiltinObjects.TryGetValue(identifier + (_ref ? "_ref" : "_obj"), out ScriptOpCode op))
            {
                switch (op)
                {
                    //true
                    case ScriptOpCode.GetInteger:
                        code = new T7OP_GetNumericValue(1, Endianess);
                        return code != null;

                    default:
                        code = new T7OpCode(op, Endianess);
                        return code != null;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to add a builtin object reference
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="_ref"></param>
        /// <returns></returns>
        public T7OpCode TryAddBuiltIn(string identifier, bool _ref)
        {
            T7OpCode code = null;

            if (TryCreateBuiltin(identifier.ToLower(), _ref, ref code))
                return __addop_internal(code);

            return null;
        }

        /// <summary>
        /// Try to add a builtin call
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public T7OpCode TryAddBuiltInCall(string identifier)
        {
            if (BuiltinFunctions.TryGetValue(identifier.ToLower(), out ScriptOpCode opcode))
                return __addop_internal(new T7OpCode(opcode, Endianess));

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
        public T7OpCode AddCallPtr(uint Context, byte NumParams)
        {
            return __addop_internal(new T7OP_CallPtr(Context, NumParams, Endianess));
        }

        /// <summary>
        /// Add a call based on a reference or namespace
        /// </summary>
        /// <param name="import"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public T7OpCode AddCall(T7Import import, uint context)
        {
            return __addop_internal(new T7OP_Call(import, context, Endianess));
        }

        /// <summary>
        /// Add a reference to an imported function
        /// </summary>
        /// <param name="import"></param>
        /// <returns></returns>
        public T7OpCode AddFunctionPtr(T7Import import)
        {
            return __addop_internal(new T7OP_GetFuncPtr(import, Endianess));
        }

        /// <summary>
        /// Add a field variable ref to the current function
        /// </summary>
        /// <param name="FunctionHash"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public T7OpCode AddFieldVariable(uint FunctionHash, uint Context)
        {
            return __addop_internal(new T7OP_EvalFieldVariable(FunctionHash, Context, Endianess));
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
        public T7OP_Jump AddJump(ScriptOpCode OpType)
        {
            T7OP_Jump jmp = new T7OP_Jump(OpType, Endianess);

            CommitJumps += jmp.CommitJump; //bind the event

            return (T7OP_Jump) __addop_internal(jmp);
        }

        /// <summary>
        /// Push loop control flow (continue, break)
        /// </summary>
        /// <param name="RefHead">Should we refer to the head of the loop, or the end.</param>
        /// <returns></returns>
        public T7OP_Jump PushLCF(bool RefHead, int offset = 0)
        {
            T7OP_Jump jmp = AddJump(ScriptOpCode.Jump);
            jmp.RefHead = RefHead;
            int RealContext = LCFContext - offset;

            if (!LCFStack.ContainsKey(RealContext))
                LCFStack[RealContext] = new List<T7OP_Jump>();

            LCFStack[RealContext].Insert(0, jmp);

            return jmp;
        }

        /// <summary>
        /// Pop a loop control flow from the stack
        /// </summary>
        /// <param name="jmp"></param>
        /// <returns></returns>
        public bool TryPopLCF(out T7OP_Jump jmp)
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
        public T7OpCode AddMathToken(string token)
        {
            if (MathTokens.TryGetValue(token, out ScriptOpCode code))
                return __addop_internal(new T7OpCode(code, Endianess));

            throw new NotImplementedException($"Math operator '{token}' has not been handled.");
        }

        /// <summary>
        /// Add a compare operator to the stack
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public T7OpCode AddCompareOp(string token)
        {
            if (CompareOps.TryGetValue(token, out ScriptOpCode code))
                return __addop_internal(new T7OpCode(code, Endianess));
            throw new NotImplementedException($"CompareOp '{token}' has not been handled.");
        }

        /// <summary>
        /// Push a foreach kvp into the local function's array
        /// </summary>
        /// <param name="keypair"></param>
        public void PushFEPair(KeyValuePair<string,string> keypair)
        {
            ForeachKeys.Add(keypair);
        }

        /// <summary>
        /// Try to pop a foreach kvp from the local function's array
        /// </summary>
        /// <param name="keypair"></param>
        /// <returns></returns>
        public bool TryPopFEPair(out KeyValuePair<string, string> keypair)
        {
            if(ForeachKeys.Count < 1)
            {
                keypair = default;
                return false;
            }

            keypair = ForeachKeys[0];
            ForeachKeys.RemoveAt(0);

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
        /// Creates a unique string of the operands passed to this. Useful in opcode prediction because we can peek opcode operands.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="CurrentPtr"></param>
        /// <param name="CurrentOp"></param>
        public static string GetOperandString(byte[] data, T7ScriptMetadata Metadata, ref uint CurrentPtr, uint MaxPtr, out ScriptOpCode CurrentOp)
        {
            CurrentOp = ScriptOpCode.Invalid;

            if (CurrentPtr >= data.Length)
                throw new IndexOutOfRangeException($"[0x{CurrentPtr.ToString("X")}]: Index of of bounds");

            ushort OpValue = BitConverter.ToUInt16(data, (int)CurrentPtr);

            CurrentPtr += 2;

            try
            {
                CurrentOp = Metadata[OpValue];
            }
            catch
            {
                throw new MissingPrimaryKeyException($"[0x{CurrentPtr.ToString("X")}]: invalid operation value '0x{OpValue.ToString("X")}'");
            }

            return ScriptOpMetadata.GenerateOpString(data, ref CurrentPtr, MaxPtr, CurrentOp);
        }

        /// <summary>
        /// Add gethash to the current function
        /// </summary>
        /// <param name="Hash"></param>
        /// <returns></returns>
        public T7OpCode AddGetHash(uint Hash)
        {
            return __addop_internal(new T7OP_GetHash(Hash, Endianess));
        }

        public override string ToString()
        {
            return $"{FunctionID:X8}[{NumParams}]@{LoadedOffset:X8}";
        }
    }
}
