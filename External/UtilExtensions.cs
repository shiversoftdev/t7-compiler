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
        /// Converts a byte array to a struct
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object ToStruct(this byte[] data, Type t)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            object val = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t);
            handle.Free();
            return val;
        }

        /// <summary>
        /// Converts a byte array to a struct, but promises that the generic constraint is met by the programmer, instead of the compiler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T ToStructUnsafe<T>(this byte[] data)
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
        /// Converts an array of structs to a byte array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_s"></param>
        /// <returns></returns>
        public static byte[] ToByteArray<T>(this T[] a_s) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] data = new byte[a_s.Length * size];
            for(int i = 0; i < a_s.Length; i++)
            {
                a_s[i].ToByteArray().CopyTo(data, i * size);
            }
            return data;
        }

        /// <summary>
        /// Converts a struct to a byte array, but promises that the generic constraint is met by the programmer, not the compiler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ToByteArrayUnsafe<T>(this T s)
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
        public unsafe static string String(this byte[] buffer, int index = default)
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

        public static string Hex(this byte[] arr)
        {
            return ByteArrayToHexViaLookup32Unsafe(arr);
        }

        // https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
        // insanity of performance nerds, but I love it
        private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        private static readonly unsafe uint* _lookup32UnsafeP = (uint*)GCHandle.Alloc(_lookup32Unsafe, GCHandleType.Pinned).AddrOfPinnedObject();
        private static uint[] CreateLookup32Unsafe()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                if (BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        }

        private unsafe static string ByteArrayToHexViaLookup32Unsafe(byte[] bytes)
        {
            var lookupP = _lookup32UnsafeP;
            var result = new char[bytes.Length * 2];
            fixed (byte* bytesP = bytes)
            fixed (char* resultP = result)
            {
                uint* resultP2 = (uint*)resultP;
                for (int i = 0; i < bytes.Length; i++)
                {
                    resultP2[i] = lookupP[bytesP[i]];
                }
            }
            return new string(result);
        }

        /// <summary>
        /// Creates an unmanaged copy of a byte array, and then returns a handle to it. Must be freed manually.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static PointerEx Unmanaged(this byte[] bytes)
        {
            IntPtr pFile = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, pFile, bytes.Length);
            return pFile;
        }
    }
}
