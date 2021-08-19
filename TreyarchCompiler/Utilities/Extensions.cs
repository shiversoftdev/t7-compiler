using System;
using System.Collections.Generic;
using System.Linq;

namespace TreyarchCompiler.Utilities
{
    internal static class Extensions
    {
        private static readonly Random rng = new Random();

        public static void Replace<T>(this List<T> list, int index, List<T> input)
        {
            list.RemoveRange(index, input.Count);
            list.InsertRange(index, input);
        }

        public static byte[] ToByteArray(this short[] arr, Enums.Games game)
        {
            if(game == Enums.Games.T6)
                return arr.Select(Convert.ToByte).ToArray();

            var bytes = new List<byte>();
            foreach (var value in arr)
                bytes.AddRange(BitConverter.GetBytes(value));
            return bytes.ToArray();
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static ushort[] GetRandomArray(int size, ushort start, ushort end)
        {
            var unique = new List<ushort>();
            while(unique.Count < size)
            {
                var value = (ushort)rng.Next(start, end);

                if(!unique.Contains(value))
                    unique.Add(value);
            }
            return unique.ToArray();
        }
    }
}
