namespace T89CompilerLib.OpCodes
{
    public class T89OP_Return : T89OpCode
    {
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
