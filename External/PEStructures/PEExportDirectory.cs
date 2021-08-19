using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.PEStructures
{
    public class PEExportDirectory : PEDataDirectory
    {
        public ImageExportDirectory ExportDirectory { get; }
        public int lpNameAddressTable { get; }
        public int lpExportAddressTable { get; }
        public PEExportDirectory(PEHeaders headers, Memory<byte> imageData) : base(headers, imageData, headers.PEHeader.ExportTableDirectory)
        {
            if (!IsValid) return;
            ExportDirectory = MemoryMarshal.Read<ImageExportDirectory>(ImageData.Span.Slice(DirectoryOffset));
            lpNameAddressTable = RelativeToOffset(ExportDirectory.lpNames);
            lpExportAddressTable = RelativeToOffset(ExportDirectory.lpExports);
        }

        public PEExport this[string exportName]
        {
            get
            {
                if (!IsValid) return null;

                // Read the export directory and name address table
                var nameAddressTable = MemoryMarshal.Cast<byte, int>(ImageData.Span.Slice(lpNameAddressTable, sizeof(int) * ExportDirectory.numNames));
                var low = 0;
                var high = ExportDirectory.numNames - 1;

                // search for name using a quicksort (lunar magic)
                while (low <= high)
                {
                    var middle = (low + high) / 2;

                    // Read the name
                    var lpName = RelativeToOffset(nameAddressTable[middle]);
                    var nameLength = ImageData.Span.Slice(lpName).IndexOf(byte.MinValue);
                    var currentName = Encoding.UTF8.GetString(ImageData.Span.Slice(lpName, nameLength).ToArray());
                    if (exportName.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Read the name ordinal table
                        var lpOrdinalTable = RelativeToOffset(ExportDirectory.lpNameOrdinals);
                        var ordinalTable = MemoryMarshal.Cast<byte, short>(ImageData.Span.Slice(lpOrdinalTable, sizeof(short) * ExportDirectory.numNames));
                        var exportOrdinal = ExportDirectory.Base + ordinalTable[middle];
                        return this[exportOrdinal];
                    }
                    if (string.CompareOrdinal(exportName, currentName) < 0) high = middle - 1;
                    else low = middle + 1;
                }

                return null;
            }
        }

        public PEExport this[int exportOrdinal]
        {
            get
            {
                if (!IsValid) return null;
                exportOrdinal -= ExportDirectory.Base;
                if (exportOrdinal >= ExportDirectory.numExports) return null;
                var addressTable = MemoryMarshal.Cast<byte, int>(ImageData.Span.Slice(lpExportAddressTable, sizeof(int) * ExportDirectory.numExports));
                var exportAddress = addressTable[exportOrdinal];

                // Check if the function is forwarded
                var exportDirectoryStartAddress = Headers.PEHeader.ExportTableDirectory.RelativeVirtualAddress;
                var exportDirectoryEndAddress = exportDirectoryStartAddress + Headers.PEHeader.ExportTableDirectory.Size;
                if (exportAddress < exportDirectoryStartAddress || exportAddress > exportDirectoryEndAddress) return new PEExport(null, exportAddress);

                // Read the forwarder string
                var forwarderStringOffset = RelativeToOffset(exportAddress);
                var forwarderStringLength = ImageData.Span.Slice(forwarderStringOffset).IndexOf(byte.MinValue);
                var forwarderString = Encoding.UTF8.GetString(ImageData.Span.Slice(forwarderStringOffset, forwarderStringLength).ToArray());
                return new PEExport(forwarderString, exportAddress);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public readonly struct ImageExportDirectory
    {
        [FieldOffset(0x10)]
        public readonly int Base;

        [FieldOffset(0x14)]
        public readonly int numExports;

        [FieldOffset(0x18)]
        public readonly int numNames;

        [FieldOffset(0x1C)]
        public readonly int lpExports;

        [FieldOffset(0x20)]
        public readonly int lpNames;

        [FieldOffset(0x24)]
        public readonly int lpNameOrdinals;
    }

    public sealed class PEExport
    { 
        public string ForwarderString { get; }
        public int RelativeAddress { get; }
        public PEExport(string forwarder, int relative)
        {
            ForwarderString = forwarder;
            RelativeAddress = relative;
        }
    }
}
