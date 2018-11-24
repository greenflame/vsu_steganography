using System.Linq;
using System.Reflection;
using BitMiracle.LibJpeg.Classic;

namespace VsuStego.Helpers
{
    public static class JpegHelper
    {
        public static JBLOCK[][] GetBuffer(jvirt_array<JBLOCK> arr)
        {
            var propertyInfo =
                typeof(jvirt_array<JBLOCK>).GetField("m_buffer", BindingFlags.Instance | BindingFlags.NonPublic);
            return (JBLOCK[][])propertyInfo?.GetValue(arr);
        }

        public static JBLOCK GetBlock(JBLOCK[][][] arr, int ind)
        {
            var pos = 0;
            var channelInd = 0;

            while (pos + arr[channelInd].GetLength() <= ind)
            {
                pos += arr[channelInd].GetLength();
            }

            ind -= pos;

            var channel = arr[channelInd];

            return channel[ind / channel.First().Length][ind % channel.First().Length];
        }

        public static int GetLength(JBLOCK[][][] arr)
        {
            return arr.Select(GetLength).Sum();
        }

        private static int GetLength(this JBLOCK[][] arr)
        {
            return arr.Length * arr.First().Length;
        }
    }
}