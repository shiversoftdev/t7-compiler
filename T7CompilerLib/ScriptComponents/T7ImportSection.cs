using System;
using System.Collections.Generic;
using System.IO;
using T7CompilerLib.OpCodes;

namespace T7CompilerLib.ScriptComponents
{
    public sealed class T7ImportSection : T7ScriptSection
    {
        private EndianType Endianess;
        private HashSet<uint> ILBuiltins = new HashSet<uint>();
        public readonly uint BuiltinHashReplacement = T7ScriptObject.Com_Hash("IsProfileBuild", 0x4B9ACE2F, 0x1000193);
        private T7ImportSection(bool littleEndian) 
        {
            Endianess = littleEndian ? EndianType.LittleEndian : EndianType.BigEndian;
            Imports = new Dictionary<ulong, T7Import>();
            LoadedOffsetPairs = new Dictionary<uint, T7Import>();
            ILBuiltins.Add(T7ScriptObject.Com_Hash("EnableOnlineMatch", 0x4B9ACE2F, 0x1000193));
        } //Prevent public initializers

        internal static T7ImportSection New(bool littleEndian)
        {
            T7ImportSection imports = new T7ImportSection(littleEndian);
            imports.Imports = new Dictionary<ulong, T7Import>();
            imports.LoadedOffsetPairs = new Dictionary<uint, T7Import>();
            return imports;
        }

        internal Dictionary<ulong, T7Import> Imports;
        public Dictionary<uint, T7Import> LoadedOffsetPairs;

        public IEnumerable<T7Import> AllImports()
        {
            foreach (var import in Imports.Values)
                yield return import;
        }
        public IEnumerable<uint> LoadOffsets()
        {
            foreach (var entry in LoadedOffsetPairs.Keys)
                yield return entry;
        }

        public bool IsBuiltinImport(uint name)
        {
            return ILBuiltins.Contains(name);
        }

        public override ushort Count()
        {
            return (ushort)Imports.Count;
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[Size()];

            EndianWriter writer = new EndianWriter(new MemoryStream(data), Endianess);

            foreach(ulong key in Imports.Keys)
            {
                var import = Imports[key];
                bool custom_builtin = IsBuiltinImport(import.Function);

                writer.Write(custom_builtin ? BuiltinHashReplacement : import.Function);
                writer.Write(import.Namespace);
                writer.Write((ushort)import.References.Count);
                writer.Write((byte)(import.NumParams + (custom_builtin ? 1 : 0)));
                writer.Write(import.Flags);

                foreach(var reference in import.References)
                {
                    writer.Write(reference.CommitAddress);
                }
            }

            writer.Dispose();

            return data;
        }

        public override uint Size()
        {
            uint count = 0;

            foreach(ulong key in Imports.Keys)
            {
                count += 12 + (uint)(Imports[key].References.Count * 4);
            }

            uint Base = GetBaseAddress();

            count = (Base + count).AlignValue(0x10) - Base;

            return count;
        }

        public T7Import AddImport(uint function, uint ns, byte paramcount, byte Flags)
        {
            if (Imports.TryGetValue(GetUnique(function, ns, paramcount, Flags), out var value))
                return value;

            T7Import import = new T7Import();
            import.Function = function;
            import.Namespace = ns;
            import.NumParams = paramcount;
            import.Flags = Flags;
            
            Imports[GetUnique(function, ns, paramcount, Flags)] = import;

            return import;
        }

        public T7Import GetImport(uint function, uint ns, byte numparams, byte Flags)
        {
            if (Imports.TryGetValue(GetUnique(function, ns, numparams, Flags), out var value))
                return value;
            return null;
        }

        public static ulong GetUnique(uint function, uint ns, byte paramcount,  byte Flags)
        {
            //very, very small collision chance. may be impossible but i haven't checked all loaded gsc namespaces.
            return (ulong)((Flags << 8) | paramcount) ^ ((ulong)function << 32 | ns); 
        }

        public static ulong GetUnique(T7Import import)
        {
            return GetUnique(import.Function, import.Namespace, import.NumParams, import.Flags);
        }

        public static void ReadImports(ref byte[] data, bool littleEndian, uint lpImportTable, ushort NumImports, ref T7ImportSection Imports)
        {
            Imports = new T7ImportSection(littleEndian);

            if (NumImports < 1)
                return;

            if (lpImportTable >= data.Length)
                throw new ArgumentException("Couldn't parse this GSC because the imports table pointer exceeded the boundaries of the data given.");

            EndianReader reader = new EndianReader(new MemoryStream(data), Imports.Endianess);
            reader.BaseStream.Position = lpImportTable;

            //bytecode loader is expected to map the correct imports. we just cache them
            for (int i = 0; i < NumImports; i++)
            {
                T7Import import = new T7Import();
                import.Function = reader.ReadUInt32();
                import.Namespace = reader.ReadUInt32();

                ushort ExpectedCount = reader.ReadUInt16();

                import.NumParams = reader.ReadByte();
                import.Flags = reader.ReadByte();

                for(int j = 0; j < ExpectedCount; j++)
                {
                    Imports.LoadedOffsetPairs[reader.ReadUInt32()] = import;
                }

                Imports.Imports[GetUnique(import.Function, import.Namespace, import.NumParams, import.Flags)] = import;
            }

            reader.Dispose();
        }

        public override void UpdateHeader(ref T7ScriptHeader Header)
        {
            Header.ImportsCount = Count();
            Header.ImportTableOffset = GetBaseAddress();
        }

        /// <summary>
        /// Get the number of imports in this imports section
        /// </summary>
        /// <returns></returns>
        public int GetNumImports()
        {
            return Imports.Count;
        }
        
    }

    public sealed class T7Import
    {
        public uint Function;
        public uint Namespace;
        public byte NumParams;
        public byte Flags;

        public override string ToString()
        {
            return $"{NumParams:X2}:{Flags:X2}";
        }

        public HashSet<T7OP_AbstractCall> References = new HashSet<T7OP_AbstractCall>();

        public static readonly HashSet<string> DevFunctions = new HashSet<string>()
        {
            "print",
            "print3d",
            "line",
            "debugstar",
            "sphere",
            "box",
            "circle",
            "assert",
            "assertmsg",
            "play",
            "printtoprightln",
            "record3dtext",
            "sphericalcone",
            "errormsg",
            "recordline",
            "adddebugcommand",
            "debug",
            "execdevgui",
            "println",
            "fprintln",
            "createprintchannel",
            "getdebugeye",
            "setprintchannel",
            "logprint",
            "recordsphere",
        };

        public ScriptOpCode ToOpCode()
        {
            if ((Flags & (byte)T7ImportFlags.IsMethod) > 0)
                return (Flags & (byte)T7ImportFlags.IsRef) > 0 ? ScriptOpCode.ScriptMethodThreadCallPointer : ScriptOpCode.ScriptMethodThreadCall;
            
            if ((Flags & (byte)T7ImportFlags.IsFunction) > 0)
                return (Flags & (byte)T7ImportFlags.IsRef) > 0 ? ScriptOpCode.ScriptThreadCallPointer : ScriptOpCode.ScriptThreadCall;

            return ScriptOpCode.GetFunction;
        }

        [Flags]
        public enum T7ImportFlags : byte
        {
            IsRef = 1,
            IsFunction = 2,
            IsMethod = 4,
            IsDebug = 16,
            NeedsResolver = 32
        }
    }
}
