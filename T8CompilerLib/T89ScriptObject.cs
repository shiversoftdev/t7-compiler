using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using T89CompilerLib.OpCodes;
using T89CompilerLib.ScriptComponents;

namespace T89CompilerLib
{
    public enum VMREVISIONS
    { 
        VM_36 = 0x36
    }
    /// <summary>
    /// T89 Script object
    /// </summary>
    public sealed class T89ScriptObject
    {
        public bool UseMasking = false;
        public VMREVISIONS VM { get; private set; }
        private static string[] HashIdentifierPrefixes = new string[]
        {
            "func_",
            "function_",
            "namespace_",
            "var_",
            "event_",
            "hash_",
            "script_"
        };
        public byte[] RawData;
        internal Dictionary<uint, string> HashMap = new Dictionary<uint, string>();

        public Dictionary<uint, string> GetHashMap()
        {
            Dictionary<uint, string> local = new Dictionary<uint, string>();
            foreach (var kvp in HashMap)
            {
                local[kvp.Key] = kvp.Value;
            }
            return local;
        }

        public T89ScriptObject(VMREVISIONS revision) : this(null, revision) { }

        public T89ScriptObject(T89ScriptMetadata NewMetadata, VMREVISIONS revision)
        {
            VM = revision;
            __header__ = T89ScriptHeader.New(this);
            __exports__ = T89ExportsSection.New(this);
            __imports__ = T89ImportSection.New(this);
            __strings__ = T89StringTableSection.New(this);
            __includes__ = T89IncludesSection.New(this);
            __globals__ = T89GlobalObjectsSection.New(this);
            if (NewMetadata != null)
                ScriptMetadata = NewMetadata;

            Link();
        }

        public T89ScriptMetadata ScriptMetadata
        {
            get
            {
                return Exports?.ScriptMetadata;
            }
            set
            {
                if (Exports != null)
                    Exports.ScriptMetadata = value;
            }
        }

        /// <summary>
        /// Script header for this object
        /// </summary>
        public T89ScriptHeader Header => __header__;
        private T89ScriptHeader __header__;

        /// <summary>
        /// Script includes
        /// </summary>
        public T89IncludesSection Includes => __includes__;
        private T89IncludesSection __includes__;

        /// <summary>
        /// Normal String Table
        /// </summary>
        public T89StringTableSection Strings => __strings__;
        private T89StringTableSection __strings__;

        /// <summary>
        /// Script function import table
        /// </summary>
        public T89ImportSection Imports => __imports__;
        private T89ImportSection __imports__;

        /// <summary>
        /// Script function export table
        /// </summary>
        public T89ExportsSection Exports => __exports__;
        private T89ExportsSection __exports__;

        /// <summary>
        /// Script function global objects table
        /// </summary>
        public T89GlobalObjectsSection Globals => __globals__;
        private T89GlobalObjectsSection __globals__;

        public byte[] Serialize()
        {
            byte[] DataBuffer = new byte[0];
            Header.Commit(ref DataBuffer, ref __header__);
            Header.CommitHeader(ref DataBuffer, 0u);
            if (UsingGSI)
            {
                EmitGSIHeader(ref DataBuffer);
            }
            return DataBuffer;
        }

        //TODO: bytecode section needs to remember to update references to strings
        public void Deserialize(byte[] data)
        {
            RawData = data;
            T89ScriptHeader.ReadHeader(ref data, ref __header__, 0u, this);
            T89ImportSection.ReadImports(ref data, Header.ImportTableOffset, Header.ImportsCount, ref __imports__, this);
            T89StringTableSection.ReadStrings(ref data, Header.StringTableOffset, Header.StringCount, ref __strings__, this);
            T89IncludesSection.ReadIncludes(ref data, Header.IncludeTableOffset, Header.IncludeCount, ref __includes__, this);
            T89ExportsSection.ReadExports(ref data, Header.ExportTableOffset, Header.ExportsCount, this, ref __exports__);
            throw new NotImplementedException("Didnt implement deserialization of __globals__");
            Link();
        }

        private void Link()
        {
            Header.Link(null, Exports);
            Exports.Link(Header, Imports);
            Imports.Link(Exports, Strings);
            Strings.Link(Imports, Includes);
            Includes.Link(Strings, Globals);
            Globals.Link(Includes, null);
        }

        /// <summary>
        /// Cast a script object to a byte array, emitting its raw data.
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator byte[](T89ScriptObject obj)
        {
            return obj.Serialize();
        }

        /// <summary>
        /// Calculate a script hash for a string input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public uint T8Hash(string input)
        {
            if (input == null)
                return 0;

            input = input.ToLower();

            if(input.Length < 1) return (uint)Unk0Hash(input);

            //if input starts with func_, var_, or hash_, use the provided hash (if possible)
            foreach (string hashprefix in HashIdentifierPrefixes)
            {
                if (input[0] != hashprefix[0] || input.Length <= hashprefix.Length)
                    continue;
                if (!input.StartsWith(hashprefix))
                    continue;
                if (!uint.TryParse(input.Substring(hashprefix.Length), NumberStyles.HexNumber, default, out uint result))
                    break;
                return result;
            }

            var val = (uint)Unk0Hash(input);
            HashMap[val] = input;
            return val;
        }

        public ulong T8s64Hash(string input)
        {
            input = input.ToLower();
            if (input.Length < 1) return 0x7FFFFFFFFFFFFFFF & HashFNV1a(Encoding.ASCII.GetBytes(input));

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

        private static ulong Unk0Hash(string input)
        {
            uint hash = 0x4B9ACE2F;
            input = input.ToLower();

            foreach (char c in input)
                hash = ((c + hash) ^ ((c + hash) << 10)) + (((c + hash) ^ ((c + hash) << 10)) >> 6);

            return 0x8001 * ((9 * hash) ^ ((9 * hash) >> 11));
        }

        private static ulong HashFNV1a(byte[] bytes, ulong fnv64Offset = 14695981039346656037, ulong fnv64Prime = 0x100000001b3)
        {
            ulong hash = fnv64Offset;

            for (var i = 0; i < bytes.Length; i++)
            {
                hash = hash ^ bytes[i];
                hash *= fnv64Prime;
            }

            return hash;
        }

        private Dictionary<string, ScriptDetour> Detours = new Dictionary<string, ScriptDetour>();

        public bool UsingGSI { private set; get; }

        /// <summary>
        /// Adds a script detour
        /// </summary>
        /// <param name="fixupNameHash">Hash of the local export to replace remote exports with</param>
        /// <param name="replaceNamespaceHash">Name of the remote export's namespace</param>
        /// <param name="replaceFunctionHash">Name of the remote export's function name</param>
        /// <param name="replaceScriptPath">Name of the remote export's script or null if it is a builtin</param>
        public void AddScriptDetour(string fixupName, string replaceNamespace, string replaceFunction, ulong replaceScriptPath)
        {
            var detour = new ScriptDetour()
            {
                FixupName = T8Hash(fixupName),
                ReplaceNamespace = T8Hash(replaceNamespace),
                ReplaceFunction = T8Hash(replaceFunction),
                ReplaceScript = replaceScriptPath
            };
            if (Detours.ContainsKey(detour.ToString()))
            {
                throw new DuplicateNameException($"Detour for {replaceNamespace}<script_{replaceScriptPath:X}>::{replaceFunction} has been defined more than once.");
            }
            Detours[detour.ToString()] = detour;
            UsingGSI = true;
        }

        public class ScriptDetour
        {
            private const int DetourNameMaxLength = 256 - 1 - (5 * 4);
            public uint FixupName;
            public uint ReplaceNamespace;
            public uint ReplaceFunction;
            public uint FixupOffset;
            public uint FixupSize;
            public ulong ReplaceScript;

            public override string ToString()
            {
                return $"{ReplaceNamespace:X}:{ReplaceFunction}:script_{ReplaceScript:X}";
            }

            public byte[] Serialize()
            {
                List<byte> toReturn = new List<byte>();
                toReturn.AddRange(BitConverter.GetBytes(FixupName));
                toReturn.AddRange(BitConverter.GetBytes(ReplaceNamespace));
                toReturn.AddRange(BitConverter.GetBytes(ReplaceFunction));
                toReturn.AddRange(BitConverter.GetBytes(FixupOffset));
                toReturn.AddRange(BitConverter.GetBytes(FixupSize));

                byte[] scriptPathBytes = new byte[DetourNameMaxLength + 1];
                BitConverter.GetBytes(ReplaceScript).CopyTo(scriptPathBytes, 0);
                toReturn.AddRange(scriptPathBytes);
                return toReturn.ToArray();
            }

            public void Deserialize(BinaryReader reader)
            {
                FixupName = reader.ReadUInt32();
                ReplaceNamespace = reader.ReadUInt32();
                ReplaceFunction = reader.ReadUInt32();
                FixupOffset = reader.ReadUInt32();
                FixupSize = reader.ReadUInt32();
                ReplaceScript = reader.ReadUInt64();
                reader.ReadBytes(DetourNameMaxLength + 1 - 8);
            }
        }

        public enum GSIFields
        {
            Detours = 0
        }

        private void EmitGSIHeader(ref byte[] data)
        {
            List<byte> NewHeader = new List<byte>();
            NewHeader.AddRange(new byte[] { (byte)'G', (byte)'S', (byte)'I', (byte)'C' });
            NewHeader.AddRange(BitConverter.GetBytes((int)0)); // num fields added

            int numFields = 0;
            // Emit Detours
            if (Detours.Count > 0)
            {
                numFields++;

                // Write the field type and the number of entries
                NewHeader.AddRange(BitConverter.GetBytes((int)GSIFields.Detours));
                NewHeader.AddRange(BitConverter.GetBytes(Detours.Count));

                foreach (ScriptDetour detour in Detours.Values)
                {
                    // Apply post-serialize information
                    detour.FixupOffset = Exports.ScriptExports[detour.FixupName].LoadedOffset;
                    detour.FixupSize = Exports.ScriptExports[detour.FixupName].LoadedSize;

                    // each detour should be exactly 256 bytes
                    NewHeader.AddRange(detour.Serialize());
                }
            }

            // copy the header
            byte[] finalData = new byte[data.Length + NewHeader.Count];
            NewHeader.ToArray().CopyTo(finalData, 0);

            // write number of fields
            BitConverter.GetBytes(numFields).CopyTo(finalData, 0x4);

            // copy the gsc script
            data.CopyTo(finalData, NewHeader.Count);
            data = finalData;
        }
    }

    /// <summary>
    /// A metadata class for T89 PC scripts
    /// All scripts need a separate static instance so that we dont load bytecode
    /// </summary>
    public class T89ScriptMetadata
    {
        private const string T89PCMetaPath = "vm_codes.db2";
        private static Dictionary<byte, byte[]> __OperationData;
        private static Dictionary<byte, byte[]> OperationData
        {
            get
            {
                if(__OperationData == null)
                {
                    try
                    {
                        __OperationData = new Dictionary<byte, byte[]>();
                        byte[] tbuff = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), T89PCMetaPath));

                        int i = 0;
                        while(i < tbuff.Length)
                        {
                            byte _vm = tbuff[i];
                            ushort count = BitConverter.ToUInt16(tbuff, i + 2);
                            __OperationData[_vm] = tbuff.Skip(i + 4).Take(count).ToArray();
                            i += 4 + count;
                        }
                    }
                    catch(Exception e)
                    {
                        throw new InvalidOperationException($"{T89PCMetaPath} could not be found in the current directory", e);
                    }
                }
                return __OperationData;
            }
        }

        /// <summary>
        /// This reverse map allows us to quickly query the defined value of a script opcode while also allowing duplicate entries for the same opcode, while also preventing two opcodes from sharing the same short value
        /// </summary>
        private readonly Dictionary<byte, Dictionary<ScriptOpCode, ushort>> ReverseOps = new Dictionary<byte, Dictionary<ScriptOpCode, ushort>>();
        private T89ScriptObject Script { get; set; }
        public T89ScriptMetadata(T89ScriptObject script) : this() { Script = script; }

        protected T89ScriptMetadata()
        {
            GenerateReverseOps();
        }

        public ushort GetOpcodeValue(ScriptOpCode code)
        {
            return this[code];
        }


        private void GenerateReverseOps()
        {
            foreach(byte key in OperationData.Keys)
            {
                ReverseOps[key] = new Dictionary<ScriptOpCode, ushort>();
                for(int i = 0; i < OperationData[key].Length; i++)
                {
                    var code = (ScriptOpCode)OperationData[key][i];
                    if(code == ScriptOpCode.Invalid)
                        continue;
                    if(ReverseOps[key].ContainsKey(code))
                        continue;
                    ReverseOps[key][code] = (ushort)i;
                }
            }
        }

        public ushort this[ScriptOpCode indexer]
        {
            get
            {
                if (ReverseOps[GetVMType()].TryGetValue(indexer, out ushort val))
                    return val;

                if (indexer == ScriptOpCode.LazyGetFunction)
                {
                    return 0x16; // hardcoded ig
                }

                Console.WriteLine($"Platform is missing opcode: {indexer.ToString()}");
                return 0xFFFF; //invalid
            }
        }

        public ScriptOpCode this[ushort indexer]
        {
            get
            {
                return (ScriptOpCode)OperationData[GetVMType()][indexer];
            }
            set
            {
                if (OperationData[GetVMType()][indexer] == (byte)ScriptOpCode.Invalid) //dont allow known rewrites
                    OperationData[GetVMType()][indexer] = (byte)value;
                else
                    throw new InvalidExpressionException("Opcode reassignment is not allowed");
            }
        }

        private byte GetVMType()
        {
            return (byte)Script.VM;
        }

        public bool ContainsKey(ushort key)
        {
            return OperationData[GetVMType()].Length > key;
        }
    }

    [Flags]
    public enum ScriptContext : uint
    {
        IsRef = 1,
        Waittill = 2,
        Threaded = 4,
        HasCaller = 8,
        DecTop = 16,
        IsPointer = 32,

    }
    [Flags]
    public enum ScriptExportFlags
    {
        None = 0x0,
        RTLoaded = 0x1,
        AutoExec = 0x2,
        Private = 0x4,
        VirtualParams = 0x20,
        Event = 0x40
    }
}
