using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public struct PointerEx
    {
        public IntPtr IntPtr { get; set; }
        public PointerEx(IntPtr value)
        {
            IntPtr = value;
        }

        #region overrides
        public static implicit operator IntPtr(PointerEx px)
        {
            return px.IntPtr;
        }

        public static implicit operator PointerEx(IntPtr ip)
        {
            return new PointerEx(ip);
        }

        public static PointerEx operator +(PointerEx px, PointerEx pxo)
        {
            return px.Add(pxo);
        }

        public static PointerEx operator -(PointerEx px, PointerEx pxo)
        {
            return px.Subtract(pxo);
        }

        public static bool operator <(PointerEx px, PointerEx pxo)
        {
            return IntPtr.Size == sizeof(int) ? ((int)px < (int)pxo) : ((long)px < (long)pxo);
        }

        public static bool operator >(PointerEx px, PointerEx pxo)
        {
            return IntPtr.Size == sizeof(int) ? ((int)px > (int)pxo) : ((long)px > (long)pxo);
        }

        public static PointerEx operator &(PointerEx px, PointerEx pxo)
        {
            return IntPtr.Size == sizeof(int) ? ((int)px & (int)pxo) : ((long)px & (long)pxo);
        }

        public static bool operator ==(PointerEx px, PointerEx pxo)
        {
            return px.IntPtr == pxo.IntPtr;
        }

        public static bool operator !=(PointerEx px, PointerEx pxo)
        {
            return px.IntPtr != pxo.IntPtr;
        }

        public static implicit operator bool(PointerEx px)
        {
            return px.IntPtr != IntPtr.Zero;
        }

        public static implicit operator byte(PointerEx px)
        {
            return (byte)px.IntPtr;
        }

        public static implicit operator sbyte(PointerEx px)
        {
            return (sbyte)px.IntPtr;
        }

        public static implicit operator int(PointerEx px)
        {
            return (int)px.IntPtr.ToInt64();
        }

        public static implicit operator uint(PointerEx px)
        {
            return (uint)px.IntPtr.ToInt64();
        }

        public static implicit operator long(PointerEx px)
        {
            return px.IntPtr.ToInt64();
        }

        public static implicit operator ulong(PointerEx px)
        {
            return (ulong)px.IntPtr.ToInt64();
        }

        public static implicit operator PointerEx(int i)
        {
            return new IntPtr(i);
        }

        public static implicit operator PointerEx(uint ui)
        {
            return new IntPtr(ui);
        }

        public static implicit operator PointerEx(long l)
        {
            return new IntPtr(l);
        }

        public static implicit operator PointerEx(ulong ul)
        {
            return new IntPtr((long)ul);
        }

        public override string ToString()
        {
            return IntPtr.ToInt64().ToString($"X{IntPtr.Size * 2}");
        }

        public PointerEx Clone()
        {
            return new PointerEx(IntPtr);
        }
        #endregion
    }

    /// <summary>
    /// A dummy type to signal that no return deserialization is needed for a call.
    /// </summary>
    public struct VOID
    {
        private PointerEx __value;
    }
}
