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
    public class PERelocationDirectory : PEDataDirectory
    {
        public PERelocationDirectory(PEHeaders headers, Memory<byte> imageData) : base(headers, imageData, headers.PEHeader.BaseRelocationTableDirectory) { }

        public IEnumerable<(int Offset, PERelocationType Type)> GetRelocations()
        {
            if (!IsValid) yield break;
            var lpCurrentRelocationBlock = DirectoryOffset;
            while (true)
            {
                // Read the relocation block
                var relocationBlock = MemoryMarshal.Read<PEImageBaseRelocation>(ImageData.Span.Slice(lpCurrentRelocationBlock));
                if (relocationBlock.SizeOfBlock == 0) break;
                var relocationCount = (relocationBlock.SizeOfBlock - Unsafe.SizeOf<PEImageBaseRelocation>()) / sizeof(short);
                for (var relocationIndex = 0; relocationIndex < relocationCount; relocationIndex++)
                {
                    // Read the relocation
                    var relocationOffset = lpCurrentRelocationBlock + Unsafe.SizeOf<PEImageBaseRelocation>() + sizeof(short) * relocationIndex;
                    var relocation = MemoryMarshal.Read<short>(ImageData.Span.Slice(relocationOffset));

                    // The type is located in the upper 4 bits of the relocation
                    var type = (ushort)relocation >> 12;

                    // The offset if located in the lower 12 bits of the relocation
                    var offset = relocation & 0xFFF;
                    yield return (RelativeToOffset(relocationBlock.VirtualAddress) + offset, (PERelocationType)type);
                }
                lpCurrentRelocationBlock += relocationBlock.SizeOfBlock;
            }
        }
    }
    public enum PERelocationType
    {
        RELOCATION_TYPE_HighLow = 0x3,
        RELOCATION_TYPE_Dir64 = 0xA
    }
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct PEImageBaseRelocation
    {
        [FieldOffset(0x0)]
        public readonly int VirtualAddress;

        [FieldOffset(0x4)]
        public readonly int SizeOfBlock;
    }
}
