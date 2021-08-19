using System.IO;
using Ionic.Zlib;

namespace TreyarchCompiler.Utilities
{
    internal class Utilities
    {
        protected static byte[] CompressBuffer(byte[] header)
        {
            using (var sm = new MemoryStream())
            {
                using (var compressor = new ZlibStream(sm, CompressionMode.Compress, CompressionLevel.BestCompression))
                {
                    compressor.Write(header, 0, header.Length);
                    compressor.Flush();
                }
                return sm.ToArray();
            }
        }
    }
}
