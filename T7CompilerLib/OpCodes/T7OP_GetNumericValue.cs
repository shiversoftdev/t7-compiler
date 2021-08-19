using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

//should be done
namespace T7CompilerLib.OpCodes
{
    public sealed class T7OP_GetNumericValue : T7OpCode
    {
        protected override ScriptOpCode Code
        {
            get; set;
        }

        private object __value__;
        private object Value
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
        public T7OP_GetNumericValue(object value, EndianType endianess) : base(endianess)
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
                case ScriptOperandType.Int32:
                    try { uint.Parse(Value.ToString()).GetBytes(Endianess).CopyTo(data, DataAddress); }
                    catch { int.Parse(Value.ToString()).GetBytes(Endianess).CopyTo(data, DataAddress); }
                    break;
                case ScriptOperandType.Float:
                    float.Parse(Value.ToString()).GetBytes(Endianess).CopyTo(data, DataAddress);
                    break;
                default:
                    if (Endianess == EndianType.BigEndian && ScriptOperandType.UInt8 == ScriptOpMetadata.OpInfo[(int)Code].OperandType)
                        new byte[] { byte.Parse(Value.ToString().Replace("-", "")) }.CopyTo(data, DataAddress);
                    else 
                        ushort.Parse(Value.ToString().Replace("-", "")).GetBytes(Endianess).CopyTo(data, DataAddress);
                    break;
            }

            return data;
        }

        public override uint GetCommitDataAddress()
        {
            switch(ScriptOpMetadata.OpInfo[(int)Code].OperandType)
            {
                case ScriptOperandType.Int32:
                case ScriptOperandType.Float:
                    return (CommitAddress + T7OP_SIZE).AlignValue(0x4);
                default:
                    if (Endianess == EndianType.BigEndian && ScriptOperandType.UInt8 == ScriptOpMetadata.OpInfo[(int)Code].OperandType)
                        return CommitAddress + T7OP_SIZE;

                    return (CommitAddress + T7OP_SIZE).AlignValue(0x2);
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
                case ScriptOperandType.Int32:
                case ScriptOperandType.Float:
                    return 4;
                case ScriptOperandType.None:
                    return 0;
                default:
                    if (Endianess == EndianType.BigEndian && ScriptOperandType.UInt8 == ScriptOpMetadata.OpInfo[(int)Code].OperandType)
                        return 1;
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

            return ScriptOpCode.GetInteger;
        }
    }
}
