using System.Collections.Generic;

namespace TreyarchCompiler.Utilities
{
    public class CompiledCode
    {
        public string Error;
        public List<string> Warning;
        public byte[] CompiledScript;
        public Dictionary<uint, byte[]> WriteData;
        public Dictionary<int, byte[]> MaskData;
        public byte[] OpcodeMap;
        public byte[] Dll;
        public Dictionary<uint, string> HashMap;

        internal CompiledCode()
        {
            Error = string.Empty;
            Warning = new List<string>();
            CompiledScript = new byte[0];
            WriteData = new Dictionary<uint, byte[]>();
            Dll = new byte[0];
            HashMap = new Dictionary<uint, string>();
        }
    }
}