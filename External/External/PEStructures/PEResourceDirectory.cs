using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace System.PEStructures
{
    public class PEResourceDirectory : PEDataDirectory
    {
        private const int ManifestResourceId = 0x18;
        private const int DllManifestId = 0x2;
        public PEResourceDirectory(PEHeaders headers, Memory<byte> imageData) : base(headers, imageData, headers.PEHeader.ResourceTableDirectory) { }

        public XDocument GetManifest()
        {
            if (!IsValid) return null;

            // Read the resource directory
            var resourceDirectory = MemoryMarshal.Read<PEImageResourceDirectory>(ImageData.Span.Slice(DirectoryOffset));
            var resourceCount = resourceDirectory.NumberOfIdEntries + resourceDirectory.NumberOfNameEntries;
            for (var resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
            {
                // Read the first level resource entry
                var firstLevelResourceEntryOffset = DirectoryOffset + Unsafe.SizeOf<PEImageResourceDirectory>() + Unsafe.SizeOf<PEImageResourceDirectoryEntry>() * resourceIndex;
                var firstLevelResourceEntry = MemoryMarshal.Read<PEImageResourceDirectoryEntry>(ImageData.Span.Slice(firstLevelResourceEntryOffset));
                if (firstLevelResourceEntry.Id != ManifestResourceId) continue;

                // Read the second level resource entry
                var secondLevelResourceEntryOffset = DirectoryOffset + Unsafe.SizeOf<PEImageResourceDirectory>() + (firstLevelResourceEntry.lpData & int.MaxValue);
                var secondLevelResourceEntry = MemoryMarshal.Read<PEImageResourceDirectoryEntry>(ImageData.Span.Slice(secondLevelResourceEntryOffset));
                if (secondLevelResourceEntry.Id != DllManifestId) continue;

                // Read the third level resource entry
                var thirdLevelResourceEntryOffset = DirectoryOffset + Unsafe.SizeOf<PEImageResourceDirectory>() + (secondLevelResourceEntry.lpData & int.MaxValue);
                var thirdLevelResourceEntry = MemoryMarshal.Read<PEImageResourceDirectoryEntry>(ImageData.Span.Slice(thirdLevelResourceEntryOffset));

                // Read the manifest data entry
                var manifestDataEntryOffset = DirectoryOffset + thirdLevelResourceEntry.lpData;
                var manifestDataEntry = MemoryMarshal.Read<PEImageResourceDataEntry>(ImageData.Span.Slice(manifestDataEntryOffset));

                // Read the manifest
                var manifestOffset = RelativeToOffset(manifestDataEntry.lpData);
                var manifest = Encoding.UTF8.GetString(ImageData.Span.Slice(manifestOffset, manifestDataEntry.Size).ToArray());

                // Sanitise the manifest to ensure it can be parsed correctly
                manifest = Regex.Replace(manifest, @"\""\""([\d\w\.]*)\""\""", @"""$1""");
                manifest = Regex.Replace(manifest, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                manifest = manifest.Replace("SXS_ASSEMBLY_NAME", @"""""");
                manifest = manifest.Replace("SXS_ASSEMBLY_VERSION", @"""""");
                manifest = manifest.Replace("SXS_PROCESSOR_ARCHITECTURE", @"""""");
                return XDocument.Parse(manifest);
            }
            return null;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct PEImageResourceDirectory
    {
        [FieldOffset(0xC)]
        public readonly short NumberOfNameEntries;

        [FieldOffset(0xE)]
        public readonly short NumberOfIdEntries;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct PEImageResourceDirectoryEntry
    {
        [FieldOffset(0x0)]
        public readonly int Id;

        [FieldOffset(0x4)]
        public readonly int lpData;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct PEImageResourceDataEntry
    {
        [FieldOffset(0x0)]
        public readonly int lpData;

        [FieldOffset(0x4)]
        public readonly int Size;
    }
}
