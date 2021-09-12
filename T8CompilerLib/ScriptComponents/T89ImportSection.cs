using System;
using System.Collections.Generic;
using System.IO;
using T89CompilerLib.OpCodes;

namespace T89CompilerLib.ScriptComponents
{
    public sealed class T89ImportSection : T89ScriptSection
    {
        public T89ScriptObject Script { get; private set; }
        private T89ImportSection(T89ScriptObject script) 
        {
            Script = script;
            Imports = new Dictionary<ulong, T89Import>();
            LoadedOffsetPairs = new Dictionary<uint, T89Import>();
        } //Prevent public initializers

        internal static T89ImportSection New(T89ScriptObject script)
        {
            T89ImportSection imports = new T89ImportSection(script);
            imports.Imports = new Dictionary<ulong, T89Import>();
            imports.LoadedOffsetPairs = new Dictionary<uint, T89Import>();
            return imports;
        }

        public Dictionary<ulong, T89Import> Imports;
        public Dictionary<uint, T89Import> LoadedOffsetPairs;

        public override ushort Count()
        {
            return (ushort)Imports.Count;
        }

        public IEnumerable<T89Import> AllImports()
        {
            foreach (var import in Imports.Values)
                yield return import;
        }
        public IEnumerable<uint> LoadOffsets()
        {
            foreach (var entry in LoadedOffsetPairs.Keys)
                yield return entry;
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[Size()];

            BinaryWriter writer = new BinaryWriter(new MemoryStream(data));

            foreach(ulong key in Imports.Keys)
            {
                var import = Imports[key];

                writer.Write(import.Function);
                writer.Write(import.Namespace);
                writer.Write((ushort)import.References.Count);
                writer.Write(import.NumParams);
                writer.Write(import.Flags);

                foreach (var reference in import.References)
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

        public T89Import AddImport(uint function, uint ns, byte paramcount, byte Flags)
        {
            if (Imports.TryGetValue(GetUnique(function, ns, paramcount, Flags), out var value))
                return value;

            T89Import import = new T89Import();
            import.Function = function;
            import.Namespace = ns;
            import.NumParams = paramcount;
            import.Flags = Flags; //todo: they really fucked this shit up
            
            Imports[GetUnique(function, ns, paramcount, Flags)] = import;

            return import;
        }

        public T89Import GetImport(uint function, uint ns, byte numparams, byte Flags)
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

        public static ulong GetUnique(T89Import import)
        {
            return GetUnique(import.Function, import.Namespace, import.NumParams, import.Flags);
        }

        public static void ReadImports(ref byte[] data, uint lpImportTable, ushort NumImports, ref T89ImportSection Imports, T89ScriptObject script)
        {
            Imports = new T89ImportSection(script);

            if (NumImports < 1)
                return;

            if (lpImportTable >= data.Length)
                throw new ArgumentException("Couldn't parse this GSC because the imports table pointer exceeded the boundaries of the data given.");

            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            reader.BaseStream.Position = lpImportTable;

            //bytecode loader is expected to map the correct imports. we just cache them
            for (int i = 0; i < NumImports; i++)
            {
                T89Import import = new T89Import();
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

        public T89Import FindLoadedImport(uint Offset)
        {
            if (!LoadedOffsetPairs.ContainsKey(Offset))
                throw new ArgumentException($"Unable to locate an import item ({Offset.ToString("X")}). Output may be incomplete");

            return LoadedOffsetPairs[Offset];
        }

        public override void UpdateHeader(ref T89ScriptHeader Header)
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

    public sealed class T89Import
    {
        public uint Function;
        public uint Namespace;
        public byte NumParams;
        public byte Flags;

        public HashSet<T89OP_AbstractCall> References = new HashSet<T89OP_AbstractCall>();

        [Flags]
        public enum T89ImportFlags : byte
        {
            IsRef = 1,
            IsFunction = 2,
            IsMethod = 4,
            IsDebug = 16,
            NeedsResolver = 32
        }
    }
}
