using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib
{
    /// <summary>
    /// Utility Functions
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// Computes the number of bytes require to pad this value
        /// </summary>
        public static int ComputePadding(int value, int alignment) => (((value) + ((alignment) - 1)) & ~((alignment) - 1)) - value;

        /// <summary>
        /// Aligns the value to the given alignment
        /// </summary>
        public static int AlignValue(this int value, int alignment) => (((value) + ((alignment) - 1)) & ~((alignment) - 1));

        public static uint AlignValue(this uint value, uint alignment) => (((value) + ((alignment) - 1)) & ~((alignment) - 1));

        /// <summary>
        /// Determines if a context value meets a desired value
        /// </summary>
        /// <param name="context"></param>
        /// <param name="desired"></param>
        /// <returns></returns>
        public static bool HasContext(this uint context, ScriptContext desired)
        {
            return (context & (uint)desired) > 0;
        }
        public static byte[] GetBytes<T>(this T input, EndianType Endianess)
        {
            byte[] data = new byte[0];
            if (input is bool) data = BitConverter.GetBytes((bool)(object)input);
            else if (input is char) data = BitConverter.GetBytes((char)(object)input);
            else if (input is double) data = BitConverter.GetBytes((double)(object)input);
            else if (input is float) data = BitConverter.GetBytes((float)(object)input);
            else if (input is int) data = BitConverter.GetBytes((int)(object)input);
            else if (input is long) data = BitConverter.GetBytes((long)(object)input);
            else if (input is short) data = BitConverter.GetBytes((short)(object)input);
            else if (input is uint) data = BitConverter.GetBytes((uint)(object)input);
            else if (input is ulong) data = BitConverter.GetBytes((ulong)(object)input);
            else data = BitConverter.GetBytes((ushort)(object)input);

            if (Endianess == EndianType.BigEndian)
                return data.Reverse().ToArray();

            return data;
        }

        /// <summary>
        /// Reads a string terminated by a null byte and returns the reader to the original position
        /// </summary>
        /// <returns>Read String</returns>
        public static string PeekNullTerminatedString(this BinaryReader br, long offset, int maxSize = -1)
        {
            // Create String Builder
            StringBuilder str = new StringBuilder();
            // Seek to position
            var temp = br.BaseStream.Position;
            br.BaseStream.Position = offset;
            // Current Byte Read
            int byteRead;
            // Size of String
            int size = 0;
            // Loop Until we hit terminating null character
            while ((byteRead = br.BaseStream.ReadByte()) != 0x0 && size++ != maxSize)
                str.Append(Convert.ToChar(byteRead));
            // Go back
            br.BaseStream.Position = temp;
            // Ship back Result
            return str.ToString();
        }

        /// <summary>
        /// Writes a null terminated string
        /// </summary>
        /// <param name="br"></param>
        /// <param name="str"></param>
        public static void WriteNullTerminatedString(this BinaryWriter br, string str)
        {
            foreach (byte c in Encoding.ASCII.GetBytes(str))
                br.Write(c);
            br.Write((byte)0);
        }

        /// <summary>
        /// Counts the number of lines in the given string
        /// </summary>
        public static int GetLineCount(string str)
        {
            int index = -1;
            int count = 0;
            while ((index = str.IndexOf(Environment.NewLine, index + 1)) != -1)
                count++;
            return count + 1;
        }

        public static string SanitiseString(string value) => value.Replace("/", "\\").Replace("\b", "\\b");
    }
}
