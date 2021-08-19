using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.PEStructures
{
    public class PELoadConfigDirectory : PEDataDirectory
    {
        public PELoadConfigDirectory(PEHeaders headers, Memory<byte> imageData) : base(headers, imageData, headers.PEHeader.LoadConfigTableDirectory) { }

        public (int HandlerCount, int RelativeAddress) GetExceptionTable()
        {
            if (!IsValid || Headers.PEHeader.DllCharacteristics.HasFlag(DllCharacteristics.NoSeh)) return (-1, -1);
            var loadConfigDirectory = MemoryMarshal.Read<PEImageLoadConfigDirectory32>(ImageData.Span.Slice(DirectoryOffset));
            return (loadConfigDirectory.SEHandlerCount, VirtualToRelative(loadConfigDirectory.SEHandlerTable));
        }

        public int GetSecurityCookieRvA()
        {
            if (!IsValid) return 0;
            long cookie;
            if (Headers.PEHeader.Magic == PEMagic.PE32) cookie = MemoryMarshal.Read<PEImageLoadConfigDirectory32>(ImageData.Span.Slice(DirectoryOffset).ToArray()).lpSecurityCookie;
            else cookie = MemoryMarshal.Read<PEImageLoadConfigDirectory64>(ImageData.Span.Slice(DirectoryOffset).ToArray()).lpSecurityCookie;
            return cookie == 0 ? 0 : VirtualToRelative(cookie);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 164)]
    public readonly struct PEImageLoadConfigDirectory32
    {
        [FieldOffset(0x3C)]
        public readonly int lpSecurityCookie;

        [FieldOffset(0x40)]
        public readonly int SEHandlerTable;

        [FieldOffset(0x44)]
        public readonly int SEHandlerCount;
    }

    [StructLayout(LayoutKind.Explicit, Size = 264)]
    public readonly struct PEImageLoadConfigDirectory64
    {
        [FieldOffset(0x58)]
        public readonly long lpSecurityCookie;
    }
}
