using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BitMiracle.LibJpeg.Classic;
using VsuStego.Helpers;

namespace VsuStego.Tasks
{
    public class Task4 : ITask
    {
        public void Run()
        {
            var input = new jpeg_decompress_struct();
            input.jpeg_stdio_src(new FileStream("flowers.jpg", FileMode.Open));
            input.jpeg_read_header(false);
            var coefficients = input.jpeg_read_coefficients();


            var channels = coefficients.Take(3).Select(JpegHelper.GetBuffer).ToArray();

            var str = "Hello, world!";

            Encrypt(channels, Encoding.ASCII.GetBytes(str));

            var decryptedData = Decrypt(channels, str.Length);
            Console.Out.WriteLine(Encoding.ASCII.GetString(decryptedData));


            var output = new jpeg_compress_struct();
            output.jpeg_stdio_dest(new FileStream("out.jpg", FileMode.Create));

            input.jpeg_copy_critical_parameters(output);

            output.jpeg_write_coefficients(coefficients);
            output.jpeg_finish_compress();
        }

        private int BytesPerGroup = 1;

        private int BytesPerBlock => BytesPerGroup * Groups.Length;

        private int[][] Groups { get; } =
        {
            new[]
            {
                0, 1, 2,
                8, 9, 10,
                16, 17, 18
            }
        };

        private Random Random { get; set; } = new Random();

        private void Encrypt(JBLOCK[][][] coefficients, byte[] data)
        {
            var l = JpegHelper.GetLength(coefficients);
            Console.Out.WriteLine($"Capacity = {l * BytesPerBlock}");

            SetPadding(data, BytesPerBlock);
            Console.Out.WriteLine("Used = {0}", data.Length);

            for (var i = 0; i < data.Length / BytesPerBlock; i++)
            {
                var block = JpegHelper.GetBlock(coefficients, i);
                Encrypt(block, data.Skip(i * BytesPerBlock).Take(BytesPerBlock).ToArray());
            }
        }

        private void Encrypt(JBLOCK block, byte[] data)
        {
            for (var i = 0; i < Groups.Length; i++)
            {
                var currentGroupData = data.Skip(i + BytesPerGroup).Take(BytesPerGroup).ToArray();
                var group = Groups[i];
                var tries = 0;

                while (!Hash(block, group).SequenceEqual(currentGroupData))
                {
                    block[group[Random.Next(group.Length)]] += (short) Random.Next(-1, 1);

                    tries++;
                    if (tries > 10000000)
                    {
                        throw new Exception("Max tries reached");
                    }
                }

                Console.WriteLine("byte encrypted");
            }
        }

        private byte[] Decrypt(JBLOCK[][][] coefficients, int length)
        {
            using (var ms = new MemoryStream())
            {
                for (var i = 0; i < length / BytesPerBlock + 1; i++)
                {
                    var d = Decrypt(JpegHelper.GetBlock(coefficients, i));
                    ms.Write(d, 0, d.Length);
                }

                return ms.GetBuffer().Take(length).ToArray();
            }
        }

        private byte[] Decrypt(JBLOCK block)
        {
            using (var ms = new MemoryStream())
            {
                foreach (var @group in Groups)
                {
                    var h = Hash(block, @group);
                    ms.Write(h, 0, h.Length);
                }

                return ms.GetBuffer();
            }
        }

        private byte[] Hash(JBLOCK block, int[] group)
        {
            using (var ms = new MemoryStream())
            using (var sw = new BinaryWriter(ms))
            using (var md5 = MD5.Create())
            {
                group.Select(i => block[i]).ToList().ForEach(sw.Write);
                return md5.ComputeHash(ms).Take(BytesPerGroup).ToArray();
            }
        }

        private static void SetPadding(byte[] data, int paddingSize)
        {
            if (data.Length % paddingSize != 0)
            {
                Array.Resize(ref data, (data.Length / paddingSize + 1) * paddingSize);
            }
        }

        private double[] Mean(JBLOCK[][][] coefficients)
        {
            var random = new Random();

            var mean = new double[64];
            var n = 0;

            foreach (var channel in coefficients)
            {
                foreach (var row in channel)
                {
                    foreach (var block in row)
                    {
                        for (var i = 0; i < 64; i++)
                        {
                            mean[i] += Math.Abs(block[i]);
                            block[i] += (short) random.Next(-10, 10);
                        }

                        n++;
                    }
                }
            }

            for (var i = 0; i < 64; i++)
            {
                mean[i] /= n;
                Console.Out.WriteLine(mean[i]);
            }

            return mean;
        }
    }
}