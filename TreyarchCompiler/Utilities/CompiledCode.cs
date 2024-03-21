using System.Collections.Generic;

namespace TreyarchCompiler.Utilities
{
    public class CompiledCode
    {
        public string Error;
        public List<string> Warning;
        public byte[] CompiledScript;
        public byte[] StubScriptData;
        public Dictionary<uint, byte[]> WriteData;
        public Dictionary<int, byte[]> MaskData;
        public byte[] OpcodeMap;
        public byte[] Dll;
        public bool RequiresGSI;
        public Dictionary<uint, string> HashMap;
        public List<uint> OpcodeEmissions;
        public string StubbedScript;

        internal CompiledCode()
        {
            Error = string.Empty;
            Warning = new List<string>();
            CompiledScript = new byte[0];
            WriteData = new Dictionary<uint, byte[]>();
            Dll = new byte[0];
            HashMap = new Dictionary<uint, string>();
            OpcodeEmissions = new List<uint>();
            StubbedScript = null;
            StubScriptData = null;
        }
    }
}