using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
