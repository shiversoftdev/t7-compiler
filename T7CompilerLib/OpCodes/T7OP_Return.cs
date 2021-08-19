using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.OpCodes
{
    public class T7OP_Return : T7OpCode
    {
        public T7OP_Return(EndianType endianess) : base(endianess) { }
        protected override ScriptOpCode Code 
        { 
            get 
            {
                if (LastOpCode != null && ScriptOpMetadata.OpInfo[(int)LastOpCode.GetOpCode()].OperandType != ScriptOperandType.None)
                    return ScriptOpCode.Return;
                return ScriptOpCode.End;
            } 
        }
    }
}
