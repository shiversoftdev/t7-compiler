using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using T7CompilerLib.OpCodes;
using T7CompilerLib.ScriptComponents;

namespace T7CompilerLib
{
    /// <summary>
    /// T7 Script object
    /// </summary>
    public sealed class T7ScriptObject
    {
        private const int MAX_METAERRORLEVEL = 1;
        private const bool AllowPeekWrites = false;
        public readonly bool LittleEndian;
        public bool UseMasking = false;

        public byte[] RawData;
        internal Dictionary<uint, string> HashMap = new Dictionary<uint, string>();

        public Dictionary<uint, string> GetHashMap()
        {
            Dictionary<uint, string> local = new Dictionary<uint, string>();
            foreach(var kvp in HashMap)
            {
                local[kvp.Key] = kvp.Value;
            }
            return local;
        }

        private static string[] HashIdentifierPrefixes = new string[]
        {
            "function_",
            "func_",
            "namespace_",
            "var_",
            "hash_"
        };

        private HashSet<uint> StatProtectedFunctions;
        private static string[] __statfunctions = new string[]
        {
        };

        private static string[] __statptrs = new string[]
        {
        };

        private Dictionary<uint, string> StatProtectedPtrs = new Dictionary<uint, string>();

        public T7ScriptObject(bool littleEndian) : this(null, littleEndian) { }

        public T7ScriptObject(T7ScriptMetadata NewMetadata, bool littleEndian)
        {
            LittleEndian = littleEndian;
            __header__ = T7ScriptHeader.New(littleEndian);
            __exports__ = T7ExportsSection.New(littleEndian, this);
            __imports__ = T7ImportSection.New(littleEndian);
            __dstrings__ = T7DebugTableSection.New(littleEndian); //not used anymore
            __strings__ = T7StringTableSection.New(littleEndian);
            __includes__ = T7IncludesSection.New(littleEndian);
            __name__ = T7NameSection.New(littleEndian);
            __strfixups__ = T7StringFixupsSection.New(__strings__);
            Detours = new Dictionary<string, ScriptDetour>();

            if (NewMetadata != null)
                ScriptMetadata = NewMetadata;
            
            StatProtectedFunctions = new HashSet<uint>();

            foreach(string func in __statfunctions)
            {
                ScriptMetadata.TryGetHash(func, out uint value);
                StatProtectedFunctions.Add(value);
            }

            foreach (string func in __statptrs)
            {
                ScriptMetadata.TryGetHash(func, out uint value);
                StatProtectedPtrs[value] = func;
            }

            Link();
        }

        public bool IsStatProtected(uint fname)
        {
            return StatProtectedFunctions.Contains(fname);
        }

        public bool IsStatPtrProtected(uint fname)
        {
            return StatProtectedPtrs.ContainsKey(fname);
        }

        public IEnumerable<string> GetStatPtrs()
        {
            foreach (string s in __statptrs)
                yield return s;
        }

        public string GetProtectionSource()
        {
            return "";
        }

        public string GetStatPtrSource()
        {
            return "";
        }

        public byte[] Randomize()
        {
            return null;
        }

        public T7ScriptMetadata ScriptMetadata
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

        public byte GetOpcodeWidth
        {
            get
            {
                return LittleEndian ? (byte)2 : (byte)1;
            }
        }

        /// <summary>
        /// Script header for this object
        /// </summary>
        public T7ScriptHeader Header => __header__;
        private T7ScriptHeader __header__;

        /// <summary>
        /// Script includes
        /// </summary>
        public T7IncludesSection Includes => __includes__;
        private T7IncludesSection __includes__;

        /// <summary>
        /// Script name
        /// </summary>
        public T7NameSection Name => __name__;
        private T7NameSection __name__;

        /// <summary>
        /// Normal String Table
        /// </summary>
        public T7StringTableSection Strings => __strings__;
        private T7StringTableSection __strings__;

        /// <summary>
        /// Debug strings table
        /// </summary>
        public T7DebugTableSection DebugStrings => __dstrings__;
        private T7DebugTableSection __dstrings__;

        /// <summary>
        /// Script function import table
        /// </summary>
        public T7ImportSection Imports => __imports__;
        private T7ImportSection __imports__;

        /// <summary>
        /// Script function import table
        /// </summary>
        public T7ExportsSection Exports => __exports__;
        private T7ExportsSection __exports__;

        public T7StringFixupsSection StringFixups => __strfixups__;
        private T7StringFixupsSection __strfixups__;

        private Dictionary<string, ScriptDetour> Detours = new Dictionary<string, ScriptDetour>();

        public bool UsingGSI { private set; get; }

        public byte[] Serialize()
        {
            byte[] DataBuffer = new byte[0];
            Header.Commit(ref DataBuffer, ref __header__);
            Header.CommitHeader(ref DataBuffer, ScriptMetadata.Magic);
            if(UsingGSI)
            {
                EmitGSIHeader(ref DataBuffer);
            }
            return DataBuffer;
        }

        //TODO: bytecode section needs to remember to update references to strings
        public void Deserialize(byte[] data)
        {
            RawData = data;
            T7ScriptHeader.ReadHeader(ref data, LittleEndian, ref __header__, ScriptMetadata.Magic);
            T7ExportsSection.ReadExports(ref data, LittleEndian, Header.ExportTableOffset, Header.ExportsCount, Header.ByteCodeOffset + Header.ByteCodeSize, ref __exports__, this);
            T7ImportSection.ReadImports(ref data, LittleEndian, Header.ImportTableOffset, Header.ImportsCount, ref __imports__);
            //T7DebugTableSection.ReadStrings(ref data, LittleEndian, Header.DebugStringTableOffset, Header.DebugStringCount, ref __dstrings__);
            T7StringTableSection.ReadStrings(ref data, LittleEndian, Header.StringTableOffset, Header.StringCount, ref __strings__);
            T7IncludesSection.ReadIncludes(ref data, LittleEndian, Header.IncludeTableOffset, Header.IncludeCount, ref __includes__);
            T7NameSection.ReadNameSection(ref data, LittleEndian, Header.NameOffset, ref __name__);

            //TODO: Write the loader for handling all those references

            Link();
        }

        private void Link()
        {
            Header.Link(null, Name);
            Name.Link(Header, Strings);
            Strings.Link(Name, Exports);
            Exports.Link(Strings, Imports);
            Imports.Link(Exports, Includes);
            Includes.Link(Imports, StringFixups);
            StringFixups.Link(Includes, null);

            //DebugStrings.Link(null, null);
        }

        /// <summary>
        /// Cast a script object to a byte array, emitting its raw data.
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator byte[] (T7ScriptObject obj)
        {
            return obj.Serialize();
        }

        /// <summary>
        /// Cast a byte array to a script object.
        /// </summary>
        /// <param name="data"></param>
        public static explicit operator T7ScriptObject((byte[] data, bool littleEndian) obj)
        {
            T7ScriptObject t = new T7ScriptObject(obj.littleEndian);

            t.Deserialize(obj.data);

            return t;
        }

        /// <summary>
        /// Calculate a script hash for a string input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public uint ScriptHash(string input)
        {
            if (input == null || input == "")
                return 0;

            input = input.ToLower();

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

            if (ScriptMetadata.TryGetHash(input, out uint value))
            {
                HashMap[value] = input;
                return value;
            }

            return 0;
        }

        internal static uint Com_Hash(string Input, uint IV, uint XORKEY)
        {
            uint hash = IV;

            foreach (char c in Input)
                hash = (char.ToLower(c) ^ hash) * XORKEY;

            hash = hash * XORKEY;

            return hash;
        }

        /// <summary>
        /// Attempt to perform a meta-translation between this script object and another misc. platform.
        /// </summary>
        /// <param name="KnownRaw"></param>
        /// <param name="UnknownRaw"></param>
        /// <param name="T7MiscObject"></param>
        public List<Exception> AttemptMetaTranslation(byte[] KnownRaw, byte[] UnknownRaw, Type ScriptType)
        {
            
            return null;
        }

        private uint[] CallOpStrToPair(string cos)
        {
            string[] split = cos.Split(':');
            return new uint[] { uint.Parse(split[0], System.Globalization.NumberStyles.HexNumber), uint.Parse(split[1], System.Globalization.NumberStyles.HexNumber) };
        }

        private struct OpPair
        {
            public ushort Value;
            public ScriptOpCode OpCode;
        }

        /// <summary>
        /// Adds a script detour
        /// </summary>
        /// <param name="fixupNameHash">Hash of the local export to replace remote exports with</param>
        /// <param name="replaceNamespaceHash">Name of the remote export's namespace</param>
        /// <param name="replaceFunctionHash">Name of the remote export's function name</param>
        /// <param name="replaceScriptPath">Name of the remote export's script or null if it is a builtin</param>
        public void AddScriptDetour(string fixupName, string replaceNamespace, string replaceFunction, string replaceScriptPath)
        {
            var detour = new ScriptDetour()
            {
                FixupName = ScriptHash(fixupName),
                ReplaceNamespace = ScriptHash(replaceNamespace),
                ReplaceFunction = ScriptHash(replaceFunction),
                ReplaceScript = replaceScriptPath
            };
            if(Detours.ContainsKey(detour.ToString()))
            {
                throw new DuplicateNameException($"Detour for {replaceNamespace}<{replaceScriptPath ?? "system"}>::{replaceFunction} has been defined more than once.");
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
            public string ReplaceScript;

            public override string ToString()
            {
                return $"{ReplaceNamespace:X}:{ReplaceFunction}:{ReplaceScript ?? "system"}";
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
                if(ReplaceScript != null)
                {
                    Encoding.ASCII.GetBytes(ReplaceScript.Substring(0, Math.Min(ReplaceScript.Length, DetourNameMaxLength))).CopyTo(scriptPathBytes, 0);
                }
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
                byte[] scriptPathBytes = reader.ReadBytes(DetourNameMaxLength + 1);
                string res = Encoding.ASCII.GetString(scriptPathBytes).Replace("\x00", "").Trim();
                if(scriptPathBytes[0] != 0)
                {
                    ReplaceScript = res;
                }
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
            if(Detours.Count > 0)
            {
                numFields++;

                // Write the field type and the number of entries
                NewHeader.AddRange(BitConverter.GetBytes((int)GSIFields.Detours));
                NewHeader.AddRange(BitConverter.GetBytes(Detours.Count));

                foreach(ScriptDetour detour in Detours.Values)
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
    /// A metadata class for T7 PC scripts
    /// All scripts need a separate static instance so that we dont load bytecode
    /// </summary>
    public class T7ScriptMetadata
    {
        private const string T7PCMetaPath = "t7pcv2.db";
        private static T7MetaV2 _pc_meta_;

        private static T7MetaV2 PCMeta
        {
            get
            {
                if (_pc_meta_ == null)
                {
                    try
                    {
                        Deserialize(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), T7PCMetaPath), out _pc_meta_);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException(
                            $"{T7PCMetaPath} could not be found in the current directory", e);
                    }
                }

                return _pc_meta_;
            }
        }

        private T7MetaV2 MetaRef;
        public ulong Magic => MetaRef.__magic;
        public uint Key => MetaRef.__key;
        public uint IV => MetaRef.__iv;
        public int Length => MetaRef.__ops.Length;
        public virtual int OpcodeWidth => 2;

        /// <summary>
        /// Only enable when editing metadata through one of the intended tools
        /// </summary>
        public bool AllowReassignment = false;

        /// <summary>
        /// This reverse map allows us to quickly query the defined value of a script opcode while also allowing duplicate entries for the same opcode, while also preventing two opcodes from sharing the same short value
        /// </summary>
        private readonly Dictionary<ScriptOpCode, ushort> ReverseOps = new Dictionary<ScriptOpCode, ushort>();

        /// <summary>
        /// Reverse map for hashes. Will only exist during metadata translation.
        /// </summary>
        private readonly Dictionary<uint, string> ReverseHashes = new Dictionary<uint, string>();

        public T7ScriptObject Script;
        public T7ScriptMetadata(T7ScriptObject obj) : this(PCMeta, obj) { } //why add vertical line space


        protected T7ScriptMetadata(T7MetaV2 __meta, T7ScriptObject obj)
        {
            Script = obj;
            MetaRef = __meta;

            if (Magic == 0)
                MetaRef.__magic = 0x1C000A0D43534780;

            //build reverse map
            for (int i = 0; i < MetaRef.__ops.Length; i++)
            {
                var value = (ScriptOpCode)MetaRef.__ops[i];
                if (!ReverseOps.ContainsKey(value))
                    ReverseOps[value] = (ushort)i;
            }

        }

        public ushort this[ScriptOpCode indexer]
        {
            get
            {
                if (ReverseOps.TryGetValue(indexer, out ushort val))
                    return val;
#if DEBUG
                Console.WriteLine($"Platform is missing opcode: {indexer.ToString()}");
#endif
                return 0xFFFF; //invalid
            }
        }

        public ScriptOpCode this[ushort indexer]
        {
            get
            {
                return (ScriptOpCode)MetaRef.__ops[indexer];
            }
            set
            {
                if (MetaRef.__ops[indexer] == (byte)ScriptOpCode.Invalid || AllowReassignment) //dont allow known rewrites
                    MetaRef.__ops[indexer] = (byte)value;
                else
                    throw new InvalidExpressionException("Opcode reassignment is not allowed");
            }
        }

        public void Set(ushort indexer, byte b)
        {
            MetaRef.__ops[indexer] = b;
        }

        public bool TryGetHash(string input, out uint Value)
        {
            Value = Com_Hash(input, MetaRef.__iv, MetaRef.__key);
            return true; //due to legacy operations
        }

        private uint Com_Hash(string input, uint iv, uint key)
        {
            uint hash = iv;

            foreach (char c in input)
                hash = (char.ToLower(c) ^ hash) * key;

            hash = hash * key;

            return hash;
        }

        public bool ContainsKey(ushort key)
        {
            return MetaRef.__ops.Length > key;
        }

        /// <summary>
        /// Serialize this metadata to disk
        /// </summary>
        /// <param name="Filepath"></param>
        public void Serialize(string Filepath)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            if (File.Exists(Filepath))
                File.Delete(Filepath);

            using (FileStream stream = File.Create(Filepath))
            {
                formatter.Serialize(stream, MetaRef);
            }
        }

        /// <summary>
        /// Deserialize a metadata databse from disk
        /// </summary>
        /// <param name="Filepath"></param>
        public static void Deserialize(string Filepath, out T7MetaV2 metastruct)
        {
            using (FileStream stream = File.OpenRead(Filepath))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                metastruct = (T7MetaV2)formatter.Deserialize(stream);
            }
        }

        protected static void InvalidOpsArray(out byte[] data)
        {
            data = new byte[0x4000];
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)ScriptOpCode.Invalid;
        }

        public ushort GetOpcodeValue(ScriptOpCode code)
        {
            return this[code];
        }
    }

    [Serializable]
    public sealed class T7MetaV2
    {
        public uint __iv = 0x4B9ACE2F;
        public uint __key = 0x1000193;
        public byte[] __ops;
        public ulong __magic;
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
        IsCustomInject = 64,
    }
    [Flags]
    public enum ScriptExportFlags
    {
        None = 0x0,
        AutoExec = 0x2,
        Private = 0x4,
    }
}