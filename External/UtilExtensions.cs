using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class UtilExtensions
    {
        /// <summary>
        /// Converts a byte array to a struct
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T ToStruct<T>(this byte[] data) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            T val = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return val;
        }

        /// <summary>
        /// Converts a struct to a byte array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ToByteArray<T>(this T s) where T : struct
        {
            PointerEx size = Marshal.SizeOf(s);
            byte[] data = new byte[size];
            PointerEx dwStruct = Marshal.AllocHGlobal((int)size);
            Marshal.StructureToPtr(s, dwStruct, true);
            Marshal.Copy(dwStruct, data, 0, size);
            Marshal.FreeHGlobal(dwStruct);
            return data;
        }

        /// <summary>
        /// Returns a null terminated byte array for the given string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] Bytes(this string s)
        {
            return Encoding.ASCII.GetBytes(s).Append<byte>(0).ToArray();
        }

        /// <summary>
        /// Gets a null terminated string at the index specified in the buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public unsafe static string String(this byte[] buffer, PointerEx index = default)
        {
            fixed (byte* bytes = &buffer[index])
            {
                return new string((sbyte*)bytes);
            }
        }

        public static bool HexByte(this string input, out byte b)
        {
            return byte.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
        }

        public static bool HexByte(this char input, out byte b)
        {
            return byte.TryParse(input.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
        }

        public static byte HexByte(this string input)
        {
            return byte.Parse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        public static byte HexByte(this char input)
        {
            return byte.Parse(input.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        public static bool IsHex(this char c)
        {
            c = char.ToLower(c);
            return (c >= 'a' && c <= 'f') || char.IsDigit(c);
        }
    }
}
