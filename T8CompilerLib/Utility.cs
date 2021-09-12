using System;
using System.IO;
using System.Text;

namespace T89CompilerLib
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
