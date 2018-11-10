using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace VsuStego.Helpers
{
    public static class BitHelper
    {
        public static bool GetBit(byte @byte, int index) => (@byte & 1 << index) != 0;

        public static void SetBit(ref byte @byte, int index, bool val) =>
            @byte = (byte) (val ? @byte | (1 << index) : @byte & ~(1 << index));

        public static byte[] Merge(List<byte[]> buffers)
        {
            var result = new byte[buffers.First().Length];

            for (var i = 0; i < result.Length; i++)
            {
                var dict = new Dictionary<byte, int>();

                foreach (var buffer in buffers)
                {
                    if (!dict.ContainsKey(buffer[i]))
                    {
                        dict.Add(buffer[i], 0);
                    }

                    dict[buffer[i]]++;
                }

                result[i] = dict.OrderByDescending(p => p.Value).First().Key;
            }

            return result;
        }
    }
}