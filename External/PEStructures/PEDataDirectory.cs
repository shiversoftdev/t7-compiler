using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace System.PEStructures
{
    public abstract class PEDataDirectory
    {
        public int DirectoryOffset { get; }
        public PEHeaders Headers { get; }
        public Memory<byte> ImageData { get; }
        public bool IsValid { get; }

        private protected PEDataDirectory(PEHeaders headers, Memory<byte> imageData, DirectoryEntry directory)
        {
            if (headers == null || headers.PEHeader == null) throw new ArgumentException("Attempted to initialize a PE data directory with null headers");
            IsValid = headers.TryGetDirectoryOffset(directory, out var directoryOffset);
            DirectoryOffset = directoryOffset;
            Headers = headers;
            ImageData = imageData;
        }

        public int RelativeToOffset(int rva)
        {
            var sectionHeader = Headers.SectionHeaders[Headers.GetContainingSectionIndex(rva)];
            return rva - sectionHeader.VirtualAddress + sectionHeader.PointerToRawData;
        }

        private protected int VirtualToRelative(int va)
        {
            return va - (int)Headers.PEHeader.ImageBase; // headers wont ever be null because we prevent it in the constructor
        }

        private protected int VirtualToRelative(long va)
        {
            return VirtualToRelative((int)va);
        }

        internal int GetPtrFromRVA(int rva, PointerEx imageBase)
        {
            PointerEx delta;

            var sectionHeader = Headers.SectionHeaders[Headers.GetContainingSectionIndex(rva)];
            delta = (sectionHeader.VirtualAddress - sectionHeader.PointerToRawData);
            return (imageBase + rva - delta);
        }
    }
}
