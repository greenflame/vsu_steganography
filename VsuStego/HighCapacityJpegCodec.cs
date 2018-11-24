using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BitMiracle.LibJpeg.Classic;
using VsuStego.Helpers;

namespace VsuStego
{
    public class HighCapacityJpegCodec
    {
        public int BytesPerGroup { get; }

        public int[][] Groups { get; }

        public int BytesPerBlock => BytesPerGroup * Groups.Length;

        private readonly MD5 _md5 = MD5.Create();

        private readonly Random _random = new Random();

        public bool UseOldEncoder { get; set; }

        public HighCapacityJpegCodec(int bytesPerGroup, int[][] groups, bool useOldEncoder = false)
        {
            BytesPerGroup = bytesPerGroup;
            Groups = groups;
            UseOldEncoder = useOldEncoder;
        }

        public void EncodeJpeg(string inputPath, string outputPath, byte[] data)
        {
            var input = new jpeg_decompress_struct();
            using (var fileStream = new FileStream(inputPath, FileMode.Open))
            {
                input.jpeg_stdio_src(fileStream);
                input.jpeg_read_header(false);
                var coefficients = input.jpeg_read_coefficients();

                var channels = coefficients.Take(3).Select(JpegHelper.GetBuffer).ToArray();
                EncodeBlocks(channels, data);

                var output = new jpeg_compress_struct();
                using (var outfile = new FileStream(outputPath, FileMode.Create))
                {
                    output.jpeg_stdio_dest(outfile);

                    input.jpeg_copy_critical_parameters(output);

                    output.jpeg_write_coefficients(coefficients);
                    output.jpeg_finish_compress();
                }
            }
        }

        public byte[] DecodeJpeg(string inputPath, int len)
        {
            var input = new jpeg_decompress_struct();
            using (var fs = new FileStream(inputPath, FileMode.Open))
            {
                input.jpeg_stdio_src(fs);
                input.jpeg_read_header(false);
                var coefficients = input.jpeg_read_coefficients();

                var channels = coefficients.Take(3).Select(JpegHelper.GetBuffer).ToArray();
                input.jpeg_finish_decompress();
                return DecodeBlocks(channels, len);
            }
        }

        private void EncodeBlocks(JBLOCK[][][] coefficients, byte[] data)
        {
            var l = JpegHelper.GetLength(coefficients);
            Console.Out.WriteLine($"Capacity = {l * BytesPerBlock}");

            EnforcePadding(data, BytesPerBlock);
            Console.Out.WriteLine("Used = {0}", data.Length);

            for (var i = 0; i < data.Length / BytesPerBlock; i++)
            {
                var block = JpegHelper.GetBlock(coefficients, i);
                EncodeBlock(block, data.Skip(i * BytesPerBlock).Take(BytesPerBlock).ToArray());

                if (i % 100 == 0)
                {
                    Console.WriteLine($"{i} bytes encrypted");
                }
            }
        }

        private byte[] DecodeBlocks(JBLOCK[][][] coefficients, int length)
        {
            using (var ms = new MemoryStream())
            {
                for (var i = 0; i < length / BytesPerBlock + 1; i++)
                {
                    var d = DecodeBlock(JpegHelper.GetBlock(coefficients, i));
                    ms.Write(d, 0, d.Length);
                }

                return ms.GetBuffer().Take(length).ToArray();
            }
        }

        private void EncodeBlock(JBLOCK block, byte[] data)
        {
            for (var i = 0; i < Groups.Length; i++)
            {
                var currentGroupData = data.Skip(i * BytesPerGroup).Take(BytesPerGroup).ToArray();

                if (UseOldEncoder)
                {
                    EncodeGroupOld(block, Groups[i], currentGroupData);
                }
                else
                {
                    EncodeGroup(block, Groups[i], currentGroupData);
                }
            }
        }

        private byte[] DecodeBlock(JBLOCK block)
        {
            using (var ms = new MemoryStream())
            {
                foreach (var @group in Groups)
                {
                    var h = DecodeGroup(block, @group);
                    ms.Write(h, 0, h.Length);
                }

                return ms.GetBuffer().Take(BytesPerBlock).ToArray();
            }
        }

        private void EncodeGroup(JBLOCK block, int[] @group, byte[] data)
        {
            var deviation = 1;

            while (!Go(block, @group, data, 0, deviation))
            {
                deviation++;
                Console.Out.WriteLine("deviation = {0}", deviation);
            }

            Console.Out.WriteLine("block encrypted");
        }

        private bool Go(JBLOCK block, int[] @group, byte[] target, int pos, int deviation)
        {
            if (pos == @group.Length)
            {
                return Hash(block, @group).SequenceEqual(target);
            }

            for (var i = -deviation; i <= deviation; i++)
            {
                block[@group[pos]] += (short) i;

                if (Go(block, @group, target, pos + 1, deviation - Math.Abs(i)))
                {
                    return true;
                }

                block[@group[pos]] -= (short) i;
            }

            return false;
        }

        private void EncodeGroupOld(JBLOCK block, int[] @group, byte[] data)
        {
            var tries = 0;

            while (!Hash(block, group).SequenceEqual(data))
            {
                var index = @group[_random.Next(@group.Length)];
                block[index] += _random.Next(2) == 0 ? (short) 1 : (short) -1;

                if (tries++ > 10000000)
                {
                    throw new Exception("Max tries reached");
                }
            }

            Console.Out.WriteLine("block encrypted");
        }

        private byte[] DecodeGroup(JBLOCK block, int[] @group)
        {
            return Hash(block, @group);
        }

        private byte[] Hash(JBLOCK block, int[] group)
        {
            using (var ms = new MemoryStream())
            using (var sw = new BinaryWriter(ms))
            {
                group.Select(i => block[i]).ToList().ForEach(sw.Write);
                sw.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                return _md5.ComputeHash(ms).Take(BytesPerGroup).ToArray();
            }
        }

        private static void EnforcePadding(byte[] buffer, int value)
        {
            if (buffer.Length % value != 0)
            {
                Array.Resize(ref buffer, (buffer.Length / value + 1) * value);
            }
        }
    }
}