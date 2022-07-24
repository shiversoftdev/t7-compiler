using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.EnvironmentEx;

namespace System
{
    internal class RPCStackFrame
    {
        private const uint ARG_ALIGN = 0x10;
        public readonly int RAXStorOffset = 0;
        private const int ReturnSize = 16;
        public readonly int ThreadStateOffset;

        /// <summary>
        /// All types that will be allowed to pass by value
        /// </summary>
        private static readonly Type[] AllowedByValue = new Type[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(bool),
            typeof(char),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(PointerEx),
            typeof(IntPtr),
            typeof(UIntPtr)
        };

        public int InternalDataOffsetEnd => ThreadStateOffset + sizeof(int);

        private List<RPCArgument> Arguments;
        private readonly int PointerSize;
        public RPCStackFrame(int pointerSize = 8)
        {
            PointerSize = pointerSize;
            ThreadStateOffset = ReturnSize;
            Arguments = new List<RPCArgument>();
        }

        public void PushArgument(dynamic obj)
        {
            if ((obj.GetType().IsValueType || (object)obj != null) && !CanSerializeType(obj.GetType()))
            {
                throw new InvalidCastException(DSTR(DSTR_SERIALIZE_TYPE_INVALID, obj.GetType().Name));
            }

            if (!obj.GetType().IsValueType && obj == null)
            {
                Arguments.Add(new RPCArgument(new byte[PointerSize]));
                return;
            }
            
            if(obj is byte[] a_b_obj)
            {
                Arguments.Add(new RPCArgument(a_b_obj, true));
                return;
            }

            if(obj is string s_obj)
            {
                Arguments.Add(new RPCArgument(s_obj));
                return;
            }

            if(obj is float f_obj)
            {
                Arguments.Add(new RPCArgument(f_obj));
                return;
            }

            if (obj is double d_obj)
            {
                Arguments.Add(new RPCArgument(d_obj));
                return;
            }

            if(obj.GetType().IsEnum)
            {
                Arguments.Add(new RPCArgument(UtilExtensions.ToByteArray(Convert.ToInt64(obj)), false));
                return;
            }
   
            if (obj.GetType().IsValueType)
            {
                var _data = UtilExtensions.ToByteArrayUnsafe(obj);

                if(IsByValue(obj.GetType()))
                {
                    Arguments.Add(new RPCArgument(_data, false));
                }
                else
                {
                    Arguments.Add(new RPCArgument(_data, true));
                }
                return;
            }

            throw new ArgumentException(DSTR(DSTR_UNHANDLED_ARG_RPC, obj.GetType().Name));
        }

        public PointerEx Size()
        {
            PointerEx n_size = InternalDataOffsetEnd;
            n_size = n_size.Align(ARG_ALIGN);
            foreach (var arg in Arguments)
            {
                if (!arg.IsReferenceType) continue;
                n_size += arg.Data.Length;
                n_size = n_size.Align(ARG_ALIGN);
            }
            return n_size;
        }

        public byte[] Build(PointerEx baseAddress)
        {
            byte[] data = new byte[Size()];
            PointerEx index = InternalDataOffsetEnd;
            index = index.Align(ARG_ALIGN);

            foreach(var arg in Arguments)
            {
                if (!arg.IsReferenceType) continue;
                arg.Handle = index + baseAddress;
                arg.Data.CopyTo(data, index);
                index += arg.Data.Length;
                index = index.Align(ARG_ALIGN);
            }

            return data;
        }

        public PointerEx GetArg(int index)
        {
            var arg = Arguments[index];
            if(arg.IsReferenceType)
            {
                if(PointerSize == 4 && (arg.IsXMM != arg.IsXMM64))
                {
                    return arg.Data.ToPointer();
                }
                return arg.Handle;
            }
            return arg.Data.ToPointer();
        }

        public bool IsArgXMM(int index)
        {
            var arg = Arguments[index];
            return arg.IsXMM;
        }

        public bool IsArgXMM64(int index)
        {
            var arg = Arguments[index];
            return arg.IsXMM64;
        }

        public bool IsArgByRef(int index, bool AllowXMM = false)
        {
            var arg = Arguments[index];
            if ((arg.IsXMM || arg.IsXMM64) && !AllowXMM) return false;
            return arg.IsReferenceType;
        }

        public static bool IsByValue(Type t)
        {
            if(!Environment.Is64BitProcess)
            {
                if(t == typeof(ulong) || t == typeof(long))
                {
                    return false;
                }
            }
            return AllowedByValue.Contains(t);
        }

        public static bool CanSerializeType(Type t)
        {
            if (t.IsValueType) return true;
            if (t == typeof(string)) return true;
            if (t == typeof(byte[])) return true;
            return false; // Serialize arrays to byte arrays first.
        }
    }

    internal class RPCArgument
    {
        /// <summary>
        ///  If an argument is a reference type, GetArg will return a pointer to the argument instead of the argument value. Non reference type arguments are not stored on the "stack"
        /// </summary>
        public readonly bool IsReferenceType;
        public PointerEx Handle;
        public bool IsXMM;
        public bool IsXMM64;
        public byte[] Data;
        public RPCArgument(byte[] data, bool isReferenceType = false)
        {
            Data = data;
            IsReferenceType = isReferenceType;
            IsXMM = false;
        }

        public RPCArgument(string s)
        {
            byte[] _data = new byte[s.Length + 1];
            Encoding.ASCII.GetBytes(s).CopyTo(_data, 0);
            Data = _data;
            IsReferenceType = true;
            IsXMM = false;
        }

        public RPCArgument(float f)
        {
            IsReferenceType = true;
            IsXMM = true;
            Data = BitConverter.GetBytes(f);
        }

        public RPCArgument(double d)
        {
            IsReferenceType = true;
            IsXMM64 = IsXMM = true;
            Data = BitConverter.GetBytes(d);
        }
    }
}
