using System;

//should be done
namespace T89CompilerLib.OpCodes
{
    public sealed class T89OP_GetNumericValue : T89OpCode
    {
        protected override ScriptOpCode Code
        {
            get; set;
        }

        private object __value__;
        public object Value
        {
            get
            {
                return __value__;
            }
            set
            {
                Code = GetTypeOfValue(value);

                if(Code == ScriptOpCode.Invalid)
                    throw new ArgumentException("A non-numeric argument was passed to a numeric opcode!");

                __value__ = value;
            }
        }
        public T89OP_GetNumericValue(object value)
        {
            Value = value;
        }

        protected override byte[] Serialize(ushort EmissionValue)
        {
            byte[] data = new byte[GetSize()];

            base.Serialize(EmissionValue).CopyTo(data, 0);

            if (Code == ScriptOpCode.GetZero)
                return data;

            uint DataAddress = GetCommitDataAddress() - CommitAddress;

            switch (ScriptOpMetadata.OpInfo[(int)Code].OperandType)
            {
                case ScriptOperandType.UInt32:
                case ScriptOperandType.Int32:
                    try { BitConverter.GetBytes(uint.Parse(Value.ToString().Replace("-", ""))).CopyTo(data, DataAddress); }
                    catch { BitConverter.GetBytes(int.Parse(Value.ToString())).CopyTo(data, DataAddress); }
                    break;
                case ScriptOperandType.Float:
                    BitConverter.GetBytes(float.Parse(Value.ToString())).CopyTo(data, DataAddress);
                    break;
                default:
                    BitConverter.GetBytes(ushort.Parse(Value.ToString().Replace("-", ""))).CopyTo(data, DataAddress);
                    break;
            }

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            switch(ScriptOpMetadata.OpInfo[(int)Code].OperandType)
            {
                case ScriptOperandType.UInt32:
                case ScriptOperandType.Int32:
                case ScriptOperandType.Float:
                    return (CommitAddress + T89OP_SIZE).AlignValue(0x4);
                default:
                    return (CommitAddress + T89OP_SIZE);
            }
        }

        public override uint GetSize()
        {
            return (GetCommitDataAddress() - CommitAddress) + GetValueSize();
        }

        public byte GetValueSize()
        {
            switch (ScriptOpMetadata.OpInfo[(int)Code].OperandType)
            {
                case ScriptOperandType.UInt32:
                case ScriptOperandType.Int32:
                case ScriptOperandType.Float:
                    return 4;
                case ScriptOperandType.None:
                    return 0;
                default:
                    return 2;
            }
        }

        private static ScriptOpCode GetTypeOfValue(object value)
        {
            if (value is double || value is float)
                return ScriptOpCode.GetFloat;

            long unknown;

            try { unknown = long.Parse(value.ToString()); }
            catch (Exception e){ Console.WriteLine(e.ToString()); return ScriptOpCode.Invalid; }

            if (unknown == 0)
                return ScriptOpCode.GetZero;

            if(unknown > 0)
            {
                if (unknown <= 255)
                    return ScriptOpCode.GetByte;

                if (unknown <= 65535)
                    return ScriptOpCode.GetUnsignedShort;
            }
            else
            {
                if (unknown >= -255)
                    return ScriptOpCode.GetNegByte;

                if (unknown >= -65535)
                    return ScriptOpCode.GetNegUnsignedShort;
            }

            return unknown > 0 ? ScriptOpCode.GetUnsignedInteger : ScriptOpCode.GetInteger;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
