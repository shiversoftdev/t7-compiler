using System;
using System.Collections.Generic;
using System.IO;
using T7CompilerLib.ScriptComponents;

namespace T7CompilerLib.OpCodes
{
    public class T7OpCode
    {
        public uint T7OP_SIZE
        { 
            get
            {
                if (Endianess == EndianType.LittleEndian)
                    return 2;
                return 1;
            }
        }

        public T7OpCode LastOpCode { get; private set; }
        public T7OpCode NextOpCode { get; private set; }

        public uint CommitAddress { get; private set; }

        protected virtual ScriptOpCode Code { get; set; }

        protected EndianType Endianess { get; private set; }

        public ScriptOpCode GetOpCode()
        {
            return Code;
        }

        public T7OpCode(ScriptOpCode op_info, EndianType endianess)
        {
            Endianess = endianess;
            Code = op_info;
        }

        protected T7OpCode(EndianType endianess) { Endianess = endianess; } //allows derived opcodes to not have to call base

        public virtual void Link(T7OpCode lastOp, T7OpCode nextOp)
        {
            LastOpCode = lastOp;
            NextOpCode = nextOp;
        }

        public T7OpCode GetEndOfChain()
        {
            if (NextOpCode == null)
                return this;

            var next = NextOpCode;

            while (next.NextOpCode != null)
                next = next.NextOpCode;

            return next;
        }

        public virtual void Append(T7OpCode NextOp)
        {
            if (NextOp == null)
                return;
            NextOpCode = NextOp;
            NextOp.LastOpCode = this;
        }

        public virtual void Insert(T7OpCode Target)
        {
            if (Target == null)
                return;

            NextOpCode = Target.NextOpCode;

            Target.NextOpCode = this;
        }

        public virtual void Unlink()
        {
            if(LastOpCode != null)
                LastOpCode.NextOpCode = NextOpCode;
            if(NextOpCode != null)
                NextOpCode.LastOpCode = LastOpCode;
        }

        protected virtual byte[] Serialize(ushort EmissionValue) //protected because we dont want outside classes calling serialize... Only needs to be protected for overrides.
        {
            if(Endianess == EndianType.LittleEndian)
                return EmissionValue.GetBytes(Endianess);
            return new byte[] { (byte)EmissionValue };
        }

        public void Commit(ref List<byte> data, ref uint BaseAddress, T7ScriptHeader header, T7ScriptMetadata EmissionTable)
        {
            CommitAddress = BaseAddress;

            //Console.WriteLine($"[OP_{Code.ToString()}] at 0x{CommitAddress.ToString("X")}, Size = 0x{GetSize().ToString("X")}");
            
            data.AddRange(Serialize(EmissionTable.GetOpcodeValue(Code)));

            BaseAddress += GetSize();
        }

        public virtual uint GetSize()
        {
            return T7OP_SIZE; //All base opcodes are only 2 bytes
        }

        public virtual uint GetCommitDataAddress()
        {
            return CommitAddress + T7OP_SIZE; //Base opcodes have no data
        }
    }

    //MONOLITHIC COPY PASTE STARTS HERE. THANKS PHIL!

    /// <summary>
    /// Script Operation Codes
    /// </summary>
    public enum ScriptOpCode : byte
    {
        End = 0x0,
        Return = 0x1,
        GetUndefined = 0x2,
        GetZero = 0x3,
        GetByte = 0x4,
        GetNegByte = 0x5,
        GetUnsignedShort = 0x6,
        GetNegUnsignedShort = 0x7,
        GetInteger = 0x8,
        GetFloat = 0x9,
        GetString = 0xA,
        GetIString = 0xB,
        GetVector = 0xC,
        GetLevelObject = 0xD,
        GetAnimObject = 0xE,
        GetSelf = 0xF,
        GetLevel = 0x10,
        GetGame = 0x11,
        GetAnim = 0x12,
        GetAnimation = 0x13,
        GetGameRef = 0x14,
        GetFunction = 0x15,
        CreateLocalVariable = 0x16,
        SafeCreateLocalVariables = 0x17,
        RemoveLocalVariables = 0x18,
        EvalLocalVariableCached = 0x19,
        EvalArray = 0x1A,
        EvalLocalArrayRefCached = 0x1B,
        EvalArrayRef = 0x1C,
        ClearArray = 0x1D,
        GetEmptyArray = 0x1E,
        GetSelfObject = 0x1F,
        EvalFieldVariable = 0x20,
        EvalFieldVariableRef = 0x21,
        ClearFieldVariable = 0x22,
        SafeSetVariableFieldCached = 0x23,
        SetWaittillVariableFieldCached = 0x24,
        ClearParams = 0x25,
        CheckClearParams = 0x26,
        EvalLocalVariableRefCached = 0x27,
        SetVariableField = 0x28,
        CallBuiltin = 0x29,
        CallBuiltinMethod = 0x2A,
        Wait = 0x2B,
        WaitTillFrameEnd = 0x2C,
        PreScriptCall = 0x2D,
        ScriptFunctionCall = 0x2E,
        ScriptFunctionCallPointer = 0x2F,
        ScriptMethodCall = 0x30,
        ScriptMethodCallPointer = 0x31,
        ScriptThreadCall = 0x32,
        ScriptThreadCallPointer = 0x33,
        ScriptMethodThreadCall = 0x34,
        ScriptMethodThreadCallPointer = 0x35,
        DecTop = 0x36,
        CastFieldObject = 0x37,
        CastBool = 0x38,
        BoolNot = 0x39,
        BoolComplement = 0x3A,
        JumpOnFalse = 0x3B,
        JumpOnTrue = 0x3C,
        JumpOnFalseExpr = 0x3D,
        JumpOnTrueExpr = 0x3E,
        Jump = 0x3F,
        JumpBack = 0x40,
        Inc = 0x41,
        Dec = 0x42,
        Bit_Or = 0x43,
        Bit_Xor = 0x44,
        Bit_And = 0x45,
        Equal = 0x46,
        NotEqual = 0x47,
        LessThan = 0x48,
        GreaterThan = 0x49,
        LessThanOrEqualTo = 0x4A,
        GreaterThanOrEqualTo = 0x4B,
        ShiftLeft = 0x4C,
        ShiftRight = 0x4D,
        Plus = 0x4E,
        Minus = 0x4F,
        Multiply = 0x50,
        Divide = 0x51,
        Modulus = 0x52,
        SizeOf = 0x53,
        WaitTillMatch = 0x54,
        WaitTill = 0x55,
        Notify = 0x56,
        EndOn = 0x57,
        VoidCodePos = 0x58,
        Switch = 0x59,
        EndSwitch = 0x5A,
        Vector = 0x5B,
        GetHash = 0x5C,
        RealWait = 0x5D,
        VectorConstant = 0x5E,
        IsDefined = 0x5F,
        VectorScale = 0x60,
        AnglesToUp = 0x61,
        AnglesToRight = 0x62,
        AnglesToForward = 0x63,
        AngleClamp180 = 0x64,
        VectorToAngles = 0x65,
        Abs = 0x66,
        GetTime = 0x67,
        GetDvar = 0x68,
        GetDvarInt = 0x69,
        GetDvarFloat = 0x6A,
        GetDvarVector = 0x6B,
        GetDvarColorRed = 0x6C,
        GetDvarColorGreen = 0x6D,
        GetDvarColorBlue = 0x6E,
        GetDvarColorAlpha = 0x6F,
        FirstArrayKey = 0x70,
        NextArrayKey = 0x71,
        ProfileStart = 0x72,
        ProfileStop = 0x73,
        SafeDecTop = 0x74,
        Nop = 0x75,
        Abort = 0x76,
        Obj = 0x77,
        ThreadObject = 0x78,
        EvalLocalVariable = 0x79,
        EvalLocalVariableRef = 0x7A,
        DevblockBegin = 0x7B,
        DevblockEnd = 0x7C,
        Breakpoint = 0x7D,
        AutoBreakpoint = 0x7E,
        ErrorBreakpoint = 0x7F,
        WatchBreakpoint = 0x80,
        NotifyBreakpoint = 0x81,
        GetObjectType,
        WaitRealTime,
        GetWorldObject,
        GetClassesObject,
        ClassFunctionCall,
        Bit_Not,
        GetWorld,
        EvalLevelFieldVariable,
        EvalLevelFieldVariableRef,
        EvalSelfFieldVariable,
        EvalSelfFieldVariableRef,
        SuperEqual,
        SuperNotEqual,
        Count,

        Invalid = 0xFF,
    }

    /// <summary>
    /// A static class to hold operation metadata
    /// </summary>
    public class ScriptOpMetadata
    {
        /// <summary>
        /// Script Operation Metadata for each operation type
        /// </summary>
        public static readonly ScriptOpMetadata[] OpInfo =
        {
            new ScriptOpMetadata(ScriptOpCode.End,                              ScriptOpType.Return,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Return,                           ScriptOpType.Return,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetUndefined,                     ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetZero,                          ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetByte,                          ScriptOpType.StackPush,         ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.GetNegByte,                       ScriptOpType.StackPush,         ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.GetUnsignedShort,                 ScriptOpType.StackPush,         ScriptOperandType.UInt16),
            new ScriptOpMetadata(ScriptOpCode.GetNegUnsignedShort,              ScriptOpType.StackPush,         ScriptOperandType.UInt16),
            new ScriptOpMetadata(ScriptOpCode.GetInteger,                       ScriptOpType.StackPush,         ScriptOperandType.Int32),
            new ScriptOpMetadata(ScriptOpCode.GetFloat,                         ScriptOpType.StackPush,         ScriptOperandType.Float),
            new ScriptOpMetadata(ScriptOpCode.GetString,                        ScriptOpType.StackPush,         ScriptOperandType.String),
            new ScriptOpMetadata(ScriptOpCode.GetIString,                       ScriptOpType.StackPush,         ScriptOperandType.String),
            new ScriptOpMetadata(ScriptOpCode.GetVector,                        ScriptOpType.StackPush,         ScriptOperandType.Vector),
            new ScriptOpMetadata(ScriptOpCode.GetLevelObject,                   ScriptOpType.Object,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetAnimObject,                    ScriptOpType.Object,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetSelf,                          ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetLevel,                         ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetGame,                          ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetAnim,                          ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetAnimation,                     ScriptOpType.StackPush,         ScriptOperandType.String),
            new ScriptOpMetadata(ScriptOpCode.GetGameRef,                       ScriptOpType.ObjectReference,   ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetFunction,                      ScriptOpType.StackPush,         ScriptOperandType.FunctionPointer),
            new ScriptOpMetadata(ScriptOpCode.CreateLocalVariable,              ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.SafeCreateLocalVariables,         ScriptOpType.None,              ScriptOperandType.VariableList),
            new ScriptOpMetadata(ScriptOpCode.RemoveLocalVariables,             ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalLocalVariableCached,          ScriptOpType.Variable,          ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.EvalArray,                        ScriptOpType.Array,             ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalLocalArrayRefCached,          ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalArrayRef,                     ScriptOpType.ArrayReference,    ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ClearArray,                       ScriptOpType.ClearVariable,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetEmptyArray,                    ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetSelfObject,                    ScriptOpType.Object,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalFieldVariable,                ScriptOpType.Variable,          ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.EvalFieldVariableRef,             ScriptOpType.VariableReference, ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.ClearFieldVariable,               ScriptOpType.ClearVariable,     ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.SafeSetVariableFieldCached,       ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.SetWaittillVariableFieldCached,   ScriptOpType.None,              ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.ClearParams,                      ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.CheckClearParams,                 ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalLocalVariableRefCached,       ScriptOpType.VariableReference, ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.SetVariableField,                 ScriptOpType.SetVariable,       ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.CallBuiltin,                      ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.CallBuiltinMethod,                ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.Wait,                             ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.WaitTillFrameEnd,                 ScriptOpType.Notification,      ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.PreScriptCall,                    ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ScriptFunctionCall,               ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.ScriptFunctionCallPointer,        ScriptOpType.Call,              ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.ScriptMethodCall,                 ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.ScriptMethodCallPointer,          ScriptOpType.Call,              ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.ScriptThreadCall,                 ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.ScriptThreadCallPointer,          ScriptOpType.Call,              ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.ScriptMethodThreadCall,           ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.ScriptMethodThreadCallPointer,    ScriptOpType.Call,              ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.DecTop,                           ScriptOpType.StackPop,          ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.CastFieldObject,                  ScriptOpType.Object,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.CastBool,                         ScriptOpType.Cast,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.BoolNot,                          ScriptOpType.Cast,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.BoolComplement,                   ScriptOpType.Cast,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.JumpOnFalse,                      ScriptOpType.JumpCondition,     ScriptOperandType.Int16),
            new ScriptOpMetadata(ScriptOpCode.JumpOnTrue,                       ScriptOpType.JumpCondition,     ScriptOperandType.Int16),
            new ScriptOpMetadata(ScriptOpCode.JumpOnFalseExpr,                  ScriptOpType.JumpExpression,    ScriptOperandType.Int16),
            new ScriptOpMetadata(ScriptOpCode.JumpOnTrueExpr,                   ScriptOpType.JumpExpression,    ScriptOperandType.Int16),
            new ScriptOpMetadata(ScriptOpCode.Jump,                             ScriptOpType.Jump,              ScriptOperandType.Int16),
            new ScriptOpMetadata(ScriptOpCode.Jump,                             ScriptOpType.Jump,              ScriptOperandType.Int16),
            new ScriptOpMetadata(ScriptOpCode.Inc,                              ScriptOpType.SingleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Dec,                              ScriptOpType.SingleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Bit_Or,                           ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Bit_Xor,                          ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Bit_And,                          ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Equal,                            ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.NotEqual,                         ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.LessThan,                         ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GreaterThan,                      ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.LessThanOrEqualTo,                ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GreaterThanOrEqualTo,             ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ShiftLeft,                        ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ShiftRight,                       ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Plus,                             ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Minus,                            ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Multiply,                         ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Divide,                           ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Modulus,                          ScriptOpType.DoubleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.SizeOf,                           ScriptOpType.SizeOf,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.WaitTillMatch,                    ScriptOpType.Notification,      ScriptOperandType.UInt8),
            new ScriptOpMetadata(ScriptOpCode.WaitTill,                         ScriptOpType.Notification,      ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Notify,                           ScriptOpType.Notification,      ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EndOn,                            ScriptOpType.Notification,      ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.VoidCodePos,                      ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Switch,                           ScriptOpType.Switch,            ScriptOperandType.Int32),
            new ScriptOpMetadata(ScriptOpCode.EndSwitch,                        ScriptOpType.SwitchCases,       ScriptOperandType.SwitchEnd),
            new ScriptOpMetadata(ScriptOpCode.Vector,                           ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetHash,                          ScriptOpType.StackPush,         ScriptOperandType.Hash),
            new ScriptOpMetadata(ScriptOpCode.RealWait,                         ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.VectorConstant,                   ScriptOpType.StackPush,         ScriptOperandType.VectorFlags),
            new ScriptOpMetadata(ScriptOpCode.IsDefined,                        ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.VectorScale,                      ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.AnglesToUp,                       ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.AnglesToRight,                    ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.AnglesToForward,                  ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.AngleClamp180,                    ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.VectorToAngles,                   ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Abs,                              ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetTime,                          ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvar,                          ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarInt,                       ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarFloat,                     ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarVector,                    ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarColorRed,                  ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarColorGreen,                ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarColorBlue,                 ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetDvarColorAlpha,                ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.FirstArrayKey,                    ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.NextArrayKey,                     ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ProfileStart,                     ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ProfileStop,                      ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.SafeDecTop,                       ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Nop,                              ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Abort,                            ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.Obj,                              ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ThreadObject,                     ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalLocalVariable,                ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalLocalVariableRef,             ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.DevblockBegin,                    ScriptOpType.None,              ScriptOperandType.UInt16),
            new ScriptOpMetadata(ScriptOpCode.DevblockEnd,                      ScriptOpType.None,              ScriptOperandType.UInt16),
            new ScriptOpMetadata(ScriptOpCode.Breakpoint,                       ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.AutoBreakpoint,                   ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ErrorBreakpoint,                  ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.WatchBreakpoint,                  ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.NotifyBreakpoint,                 ScriptOpType.None,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetObjectType,                    ScriptOpType.StackPush,         ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.WaitRealTime,                     ScriptOpType.Call,              ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetWorldObject,                   ScriptOpType.Object,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetClassesObject,                 ScriptOpType.Object,            ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.ClassFunctionCall,                ScriptOpType.Call,              ScriptOperandType.Call),
            new ScriptOpMetadata(ScriptOpCode.Bit_Not,                          ScriptOpType.SingleOperand,     ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.GetWorld,                         ScriptOpType.StackPush,         ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.EvalLevelFieldVariable,           ScriptOpType.Variable,          ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.EvalLevelFieldVariableRef,        ScriptOpType.VariableReference, ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.EvalSelfFieldVariable,            ScriptOpType.Variable,          ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.EvalSelfFieldVariableRef,         ScriptOpType.VariableReference, ScriptOperandType.VariableName),
            new ScriptOpMetadata(ScriptOpCode.SuperEqual,                       ScriptOpType.Comparison,        ScriptOperandType.None),
            new ScriptOpMetadata(ScriptOpCode.SuperNotEqual,                    ScriptOpType.Comparison,        ScriptOperandType.None),
        };

        /// <summary>
        /// Eat the data section of an opcode and return a unique string representation of it
        /// </summary>
        /// <param name="CurrentPtr"></param>
        /// <param name="code"></param>
        public static string GenerateOpString(byte[] data, ref uint CurrentPtr, uint MaxPtr, ScriptOpCode code)
        {
           
            return null;
        }

        private static void LoadEndSwitch(byte[] data, ref uint CurrentPtr, uint MaxPtr)
        {
            
        }

        /// <summary>
        /// Gets or Sets the Operation Code
        /// </summary>
        public ScriptOpCode OpCode { get; private set; }

        /// <summary>
        /// Gets or Sets the Operation Type
        /// </summary>
        public ScriptOpType OpType { get; private set; }

        /// <summary>
        /// Gets or Sets the Operand Data Type/s
        /// </summary>
        public ScriptOperandType OperandType { get; private set; }

        /// <summary>
        /// Initializes and instance of the Metadata Class with the given info
        /// </summary>
        public ScriptOpMetadata(ScriptOpCode opCode, ScriptOpType opType, ScriptOperandType operandType)
        {
            OpCode = opCode;
            OpType = opType;
            OperandType = operandType;
        }
    }

    /// <summary>
    /// Script Operation Types
    /// </summary>
    public enum ScriptOperandType : byte
    {
        None,
        Int8,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float,
        Vector,
        VectorFlags,
        String,
        Call,
        FunctionPointer,
        Hash,
        VariableList,
        VariableName,
        VariableIndex,
        SwitchEnd,
    }

    /// <summary>
    /// Script Operation Types
    /// </summary>
    public enum ScriptOpType
    {
        None,
        StackPush,
        StackPop,
        Endon,
        Notification,
        Waittill,
        Call,
        JumpExpression,
        JumpCondition,
        Jump,
        SetVariable,
        Variable,
        VariableReference,
        Array,
        ArrayReference,
        ClearVariable,
        Object,
        ObjectReference,
        Cast,
        SingleToken,
        SingleOperand,
        DoubleOperand,
        Comparison,
        SizeOf,
        Switch,
        SwitchCases,
        Return,
    }

}
