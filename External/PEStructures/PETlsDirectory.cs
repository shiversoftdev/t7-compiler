using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.PEStructures
{
    public class PETlsDirectory : PEDataDirectory
    {
        public PETlsDirectory(PEHeaders headers, Memory<byte> imageData) : base(headers, imageData, headers.PEHeader.ThreadLocalStorageTableDirectory) { }
        internal IEnumerable<int> GetTlsCallbackPointers()
        {
            if (!IsValid) yield break;
            bool is32bit = Headers.PEHeader.Magic == PEMagic.PE32;

            // Read the TLS directory
            long cbAddress;
            if (is32bit) cbAddress = MemoryMarshal.Read<PEImageTlsDirectory32>(ImageData.Span.Slice(DirectoryOffset)).AddressOfCallBacks;
            else cbAddress = MemoryMarshal.Read<PEImageTlsDirectory64>(ImageData.Span.Slice(DirectoryOffset)).AddressOfCallBacks;

            if (cbAddress == 0) yield break;
            var callbackIndex = 0;
            while (true)
            {
                // Read the callback address
                var callbackAddressOffset = RelativeToOffset(VirtualToRelative(cbAddress)) + (is32bit? sizeof(int) : sizeof(long)) * callbackIndex;
                long callbackAddress = is32bit ? MemoryMarshal.Read<int>(ImageData.Span.Slice(callbackAddressOffset)) : MemoryMarshal.Read<long>(ImageData.Span.Slice(callbackAddressOffset));
                if (callbackAddress == 0) yield break;
                yield return VirtualToRelative(callbackAddress);
                callbackIndex++;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly struct PEImageTlsDirectory32
    {
        [FieldOffset(0xC)]
        public readonly int AddressOfCallBacks;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    internal readonly struct PEImageTlsDirectory64
    {
        [FieldOffset(0x18)]
        public readonly long AddressOfCallBacks;
    }
}
