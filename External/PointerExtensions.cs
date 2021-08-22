using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.EnvironmentEx;

namespace System
{
    public static class PointerExtensions
    {
        public static IntPtr Add(this IntPtr i, IntPtr offset)
        {
            if (IntPtr.Size == sizeof(int)) return IntPtr.Add(i, offset.ToInt32());
            return new IntPtr(i.ToInt64() + offset.ToInt64());
        }

        public static PointerEx Add(this PointerEx i, PointerEx offset)
        {
            return i.IntPtr.Add(offset);
        }

        public static IntPtr Subtract(this IntPtr i, IntPtr offset)
        {
            if (IntPtr.Size == sizeof(int)) return IntPtr.Subtract(i, offset.ToInt32());
            return new IntPtr(i.ToInt64() - offset.ToInt64());
        }

        public static PointerEx Subtract(this PointerEx i, PointerEx offset)
        {
            return i.IntPtr.Subtract(offset);
        }

        public static PointerEx Align(this PointerEx value, uint alignment) => (value + (alignment - 1)) & ~(alignment - 1);

        public static PointerEx ToPointer(this byte[] data)
        {
            if (IntPtr.Size < data.Length)
            {
                throw new InvalidCastException(DSTR(DSTR_PTR_CAST_FAIL, data.Length, IntPtr.Size));
            }

            if(data.Length < IntPtr.Size)
            {
                byte[] _data = new byte[IntPtr.Size];
                data.CopyTo(_data, 0);
                data = _data;
            }

            if (IntPtr.Size == sizeof(long))
            {
                return BitConverter.ToInt64(data, 0);
            }

            return BitConverter.ToInt32(data, 0);
        }
    }
}
