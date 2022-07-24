using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.PEStructures.TLB
{
    /// <summary>
    /// A C# parser for type libraries. Thanks to Tony Burcham and TheirCorp for the unofficial specs (https://forum.powerbasic.com/forum/user-to-user-discussions/source-code/39586-the-unofficial-typelib-data-format-specification)
    /// </summary>
    public sealed class TypeLib
    {
        private delegate void SegmentParser(TypeLib lib, BinaryReader reader);
        private TLBHeader Header;
        private int[] TypeInfoOffsets; // not quite sure what these are for?
        private SegmentDescriptor[] SegmentDescriptors;
        private static Dictionary<TLBSegment, SegmentParser> SegmentParsingHandlers;

        public TypeInfo[] TypeInfos;
        public ImportInfo[] ImportInfos;

        public TypeLib(byte[] rawfile)
        {
            using (var mstream = new MemoryStream(rawfile))
            {
                using(var reader = new BinaryReader(mstream))
                {
                    Header = rawfile.ToStruct<TLBHeader>();
                    reader.BaseStream.Position += Marshal.SizeOf(typeof(TLBHeader)) - (Header.HasTypeLibFileName() ? 0 : 4);

                    TypeInfoOffsets = new int[Header.NumTypeInfos];

                    for(int i = 0; i < Header.NumTypeInfos; i++)
                    {
                        TypeInfoOffsets[i] = reader.ReadInt32();
                    }

                    SegmentDescriptors = new SegmentDescriptor[15];

                    for (int i = 0; i < 15; i++)
                    {
                        SegmentDescriptors[i] = reader.ReadBytes(Marshal.SizeOf(typeof(SegmentDescriptor))).ToStruct<SegmentDescriptor>();
                    }

                    foreach(TLBSegment type in Enum.GetValues(typeof(TLBSegment)))
                    {
                        if(!SegmentParsingHandlers.ContainsKey(type))
                        {
                            throw new NotImplementedException($"Tried to parse section '{type}' but it is unimplemented");
                        }
                        SegmentParsingHandlers[type](this, reader);
                    }
                }
            }
        }

        private static void ParseTypeInfo(TypeLib @this, BinaryReader reader)
        {
            @this.TypeInfos = new TypeInfo[@this.Header.NumTypeInfos];

            var segmentBase = @this.SegmentDescriptors[(int)TLBSegment.TypeInfoTable].SegmentOffset;
            reader.BaseStream.Position = segmentBase;

            for(int i = 0; i < @this.TypeInfos.Length; i++)
            {
                @this.TypeInfos[i] = reader.ReadBytes(Marshal.SizeOf(typeof(TypeInfo))).ToStruct<TypeInfo>();
            }
        }

        private static void ParseImportInfo(TypeLib @this, BinaryReader reader)
        {
            @this.ImportInfos = new ImportInfo[@this.Header.NumImportInfos];

            var segmentBase = @this.SegmentDescriptors[(int)TLBSegment.ImportInfo].SegmentOffset;
            reader.BaseStream.Position = segmentBase;

            for (int i = 0; i < @this.TypeInfos.Length; i++)
            {
                @this.ImportInfos[i] = reader.ReadBytes(Marshal.SizeOf(typeof(ImportInfo))).ToStruct<ImportInfo>();
            }
        }

        static TypeLib()
        {
            SegmentParsingHandlers[TLBSegment.TypeInfoTable] = ParseTypeInfo;
            SegmentParsingHandlers[TLBSegment.ImportInfo] = ParseImportInfo;
        }

        private TypeLib()
        {
            throw new NotImplementedException();
        }
    }

    public enum TLBHeaderFlags
    {
        HelpFileDefined = 1 << 5,
        TypeLibFilenamePresent = 1 << 8
    }

    public struct TLBHeader
    {
        /// <summary>
        /// "MSFT" (0x5446534D) OR SLGT, SLGT not supported
        /// </summary>
        public int FileMagic; // 0x0

        /// <summary>
        /// expected to be 0x00010002
        /// </summary>
        public int FormatVersion; // 0x4

        /// <summary>
        /// GUID Offset into GUID table, otherwise, -1
        /// </summary>
        public int GUIDOffset; // 0x8

        /// <summary>
        /// The locale ID, for example 0x0409 = "USA", "English United States"
        /// </summary>
        public int LocaleID; // 0xC

        /// <summary>
        /// Unk
        /// </summary>
        public int LocaleID2; // 0x10

        /// <summary>
        /// [0-3] SysKind, [5] helpfile defined, [8] if typelib file name field is present, rest unk
        /// </summary>
        public int VarFlags; // 0x14

        /// <summary>
        /// Set with SetVersion()
        /// </summary>
        public int Version; // 0x18

        /// <summary>
        /// Set with setflags
        /// </summary>
        public int Flags; // 0x1C

        /// <summary>
        /// Number of TypeInfos present in the file
        /// </summary>
        public int NumTypeInfos; // 0x20

        /// <summary>
        /// Offset into string table for helpstring
        /// </summary>
        public int HelpStringOffset; // 0x24

        /// <summary>
        /// ????
        /// </summary>
        public int HelpStringContext; // 0x28

        /// <summary>
        /// ????
        /// </summary>
        public int HelpContext; // 0x2C

        /// <summary>
        /// Number of names in the name table
        /// </summary>
        public int NumNames; // 0x30

        /// <summary>
        /// Number of chars in name table
        /// </summary>
        public int NameTableCharCount; // 0x34

        /// <summary>
        /// Offset into string table for the Typelib's name
        /// </summary>
        public int TypeLibNameOffset; // 0x38

        /// <summary>
        /// Offset into string table for Help File name
        /// </summary>
        public int HelpFileNameOffset; // 0x3C

        /// <summary>
        /// Offset to the GUID table or custom data if present, otherwise, -1
        /// </summary>
        public int CustomDataOffset; // 0x40

        public int Unk44;
        public int Unk48;

        /// <summary>
        /// hRefType to IDispatch, or -1 if there's no IDispatch
        /// </summary>
        public int DispatchPostiion; // 0x4C

        /// <summary>
        /// Number of ImpInfo structures
        /// </summary>
        public int NumImportInfos; // 0x50

        /// <summary>
        /// May not exist. Depends on VarFlags and (1 sl 8). Offset into String Table of TypeLib file name.
        /// </summary>
        public int TypeLibFileNameOffset_Q; // 0x54

        public bool HasTypeLibFileName()
        {
            return 0 != ((int)TLBHeaderFlags.TypeLibFilenamePresent & VarFlags);
        }
    }

    public struct SegmentDescriptor
    {
        /// <summary>
        /// File offset to segment info
        /// </summary>
        public int SegmentOffset; // 0x0

        /// <summary>
        /// Size of the segment
        /// </summary>
        public int Size; // 0x4

        public int Unk8; // 0x8
        public int UnkC; // 0xC
    }

    public enum TLBSegment
    {
        TypeInfoTable = 0,
        ImportInfo = 1,
        ImportedFiles = 2,
        ReferencesTable = 3,
        LibTable = 4,
        GUIDTable = 5,
        NameTable = 7,
        StringTable = 8,
        TypeDescriptors = 9,
        ArrayDescriptors = 10,
        CustomData = 11,
        GUIDOffsets = 12
    }

    public enum TypeKinds
    {
        TKIND_ENUM = 0,
        TKIND_RECORD = 1,
        TKIND_MODULE = 2,
        TKIND_INTERFACE = 3,
        TKIND_DISPATCH = 4,
        TKIND_COCLASS = 5,
        TKIND_ALIAS = 6,
        TKIND_UNION = 7,
        TKIND_MAX = 8
    }

    public enum TypeFlags
    {
        TYPEFLAG_FAPPOBJECT = 0x00001,
        TYPEFLAG_FCANCREATE = 0x00002,
        TYPEFLAG_FLICENSED = 0x00004,
        TYPEFLAG_FPREDECLID = 0x00008,
        TYPEFLAG_FHIDDEN = 0x00010,
        TYPEFLAG_FCONTROL = 0x00020,
        TYPEFLAG_FDUAL = 0x00040,
        TYPEFLAG_FNONEXTENSIBLE = 0x00080,
        TYPEFLAG_FOLEAUTOMATION = 0x00100,
        TYPEFLAG_FRESTRICTED = 0x00200,
        TYPEFLAG_FAGGREGATABLE = 0x00400,
        TYPEFLAG_FREPLACEABLE = 0x00800,
        TYPEFLAG_FDISPATCHABLE = 0x01000,
        TYPEFLAG_FREVERSEBIND = 0x02000
    }

    public struct TypeInfo
    {
        /// <summary>
        /// The lower 4 bits are the TypeKinds code. Bits 11 through 15 contain an alignment value
        /// </summary>
        public int TypeKind; // 0x0

        /// <summary>
        /// File offset to an array of funcs and props. If no funcs or props, FileLength + 1.
        /// </summary>
        public int FunctionRecordsOffset; // 0x4

        /// <summary>
        /// Size of memory required for allocating the type (or 0).
        /// </summary>
        public int MemAllocationSize; // 0x8

        /// <summary>
        /// Size of reconstituded typeinfo or -1 if no element.
        /// </summary>
        public int ReconstitutedSize; // 0xC

        public int Unk10; // always 3
        public int Unk14; // always 0

        public ushort NumFuncs; // 0x18
        public ushort NumProps; // 0x1A

        public int Unk1C;
        public int Unk20;
        public int Unk24;
        public int Unk28;

        /// <summary>
        /// Offset into GUID table for type GUID
        /// </summary>
        public int GUIDOff; // 0x2C

        /// <summary>
        /// TypeFlags flags
        /// </summary>
        public int TypeFlags; // 0x30

        /// <summary>
        /// Offset into the name table for the name of the type.
        /// </summary>
        public int NameOffset; // 0x34

        /// <summary>
        /// Element version
        /// </summary>
        public int Version; // 0x38

        /// <summary>
        /// Offset into string table for the docstring (or -1)
        /// </summary>
        public int DocStringOff; // 0x3C

        /// <summary>
        /// ????
        /// </summary>
        public int HelpStringContext; // 0x40

        /// <summary>
        /// ????
        /// </summary>
        public int HelpContext; // 0x44

        /// <summary>
        /// Offset into custom data table of data for this element
        /// </summary>
        public int CustomDataOff; // 0x48

        /// <summary>
        /// Count of implemented interfaces
        /// </summary>
        public ushort NumImplementedInterfaces; // 0x4C

        /// <summary>
        /// Size of the vtable not including inheritance
        /// </summary>
        public ushort VirtualTableSize; // 0x4E

        public int Unk50;

        /// <summary>
        /// Offset into Type Descriptor Table, or in base interfaces.
        /// If TypeKind is a coclass, then DataValue is an offset into RefTable.
        /// If TypeKind is a interface, then DataType1 is a reference to inherited interface.
        /// If TypeKind is a module, then DataType1 is an offset into the Name Table of the DLL name.
        /// </summary>
        public int DataValue; // 0x54

        /// <summary>
        /// If 0x8000, entry above is valid, else it is zero
        /// </summary>
        public int DataFlags; // 0x58

        public int Unk5C; // always 0
        public int Unk60; // always -1
    }

    public struct ImportInfo
    {
        public ushort Count; // 0x0
        public byte Flags; // 0x2
        public byte TypeKind; // 0x3
        public int ImportFileOffset; // 0x4
        public int GUIDOffset; // 0x8
    }
}
