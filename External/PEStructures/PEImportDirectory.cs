using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.PEStructures
{
    public class PEImportDirectory : PEDataDirectory
    {
        public PEImportDirectory(PEHeaders headers, Memory<byte> imageData) : base(headers, imageData, headers.PEHeader.ImportTableDirectory) { }
        public IEnumerable<PEImportDescriptor> GetImportDescriptors()
        {
            if (!IsValid) yield break;
            var descriptorIndex = 0;
            while(true)
            {
                // Read the descriptor
                var lpDescriptor = DirectoryOffset + Unsafe.SizeOf<PEImageImportDescriptor>() * descriptorIndex;
                var descriptor = MemoryMarshal.Read<PEImageImportDescriptor>(ImageData.Span.Slice(lpDescriptor));
                if (descriptor.FirstThunk == 0) break;

                // Read the descriptor name
                var lpDescriptorName = RelativeToOffset(descriptor.Name);
                var descriptorNameLength = ImageData.Span.Slice(lpDescriptorName).IndexOf(byte.MinValue);
                var descriptorName = Encoding.UTF8.GetString(ImageData.Span.Slice(lpDescriptorName, descriptorNameLength).ToArray());

                // Read the imports described
                var lpOffsetTable = RelativeToOffset(descriptor.FirstThunk);
                var lpThunkTable = descriptor.OriginalFirstThunk == 0 ? lpOffsetTable : RelativeToOffset(descriptor.OriginalFirstThunk);
                var imports = GetImports(lpOffsetTable, lpThunkTable);
                yield return new PEImportDescriptor(imports, descriptorName);
                descriptorIndex += 1;
            }
        }

        private IEnumerable<PEImport> GetImports(int lpOffsetTable, int lpThunkTable)
        {
            var importIndex = 0;
            bool is32bit = Headers.PEHeader.Magic == PEMagic.PE32;
            do
            {
                // Read the import thunk
                var functionOffset = lpOffsetTable + (is32bit ? sizeof(int) : sizeof(long)) * importIndex;
                var importThunkOffset = lpThunkTable + (is32bit ? sizeof(int) : sizeof(long)) * importIndex;
                PointerEx importThunk = is32bit ? MemoryMarshal.Read<int>(ImageData.Span.Slice(importThunkOffset)) : MemoryMarshal.Read<long>(ImageData.Span.Slice(importThunkOffset));
                if (!importThunk) break;

                int importOrdinal;
                // Check if the function is imported via ordinal
                if ((importThunk & int.MinValue) != 0)
                {
                    importOrdinal = importThunk & (long)ushort.MaxValue;
                    yield return new PEImport(null, functionOffset, importOrdinal);
                    continue;
                }

                // Read the import ordinal and name
                var lpImportOrdinal = RelativeToOffset(importThunk);
                importOrdinal = MemoryMarshal.Read<short>(ImageData.Span.Slice(lpImportOrdinal));
                var lpImportName = lpImportOrdinal + sizeof(short);
                var importNameLength = ImageData.Span.Slice(lpImportName).IndexOf(byte.MinValue);
                var importName = Encoding.UTF8.GetString(ImageData.Span.Slice(lpImportName, importNameLength).ToArray());
                yield return new PEImport(importName, functionOffset, importOrdinal);
            }
            while ((importIndex += 1) > 0);
        }
    }

    public sealed class PEImportDescriptor
    {
        public IEnumerable<PEImport> Imports { get; }
        public string Name { get; }
        public PEImportDescriptor(IEnumerable<PEImport> imports, string name)
        {
            Imports = imports;
            Name = name;
        }
    }
    
    public sealed class PEImport
    {
        public string Name { get; }
        public int Offset { get; }
        public int Ordinal { get; }
        public PEImport(string name, int offset, int ordinal)
        {
            Name = name;
            Offset = offset;
            Ordinal = ordinal;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public readonly struct PEImageImportDescriptor
    {
        [FieldOffset(0x0)]
        public readonly int OriginalFirstThunk;

        [FieldOffset(0xC)]
        public readonly int Name;

        [FieldOffset(0x10)]
        public readonly int FirstThunk;
    }
}
