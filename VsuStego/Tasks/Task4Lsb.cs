using System;
using System.IO;
using System.Linq;
using System.Text;
using BitMiracle.LibJpeg.Classic;
using VsuStego.Helpers;

namespace VsuStego.Tasks
{
    public class Task4Lsb : ITask
    {
        public void Run()
        {
            const string str = "Hello, world!";

            Enc("flowers.jpg", "out.jpg", Encoding.ASCII.GetBytes(str));

            Console.Out.WriteLine(Encoding.ASCII.GetString(Dec("out.jpg", str.Length)));
        }

        private void Enc(string inputPath, string outputPath, byte[] data)
        {
            var input = new jpeg_decompress_struct();
            using (var fileStream = new FileStream(inputPath, FileMode.Open))
            {
                input.jpeg_stdio_src(fileStream);
                input.jpeg_read_header(false);
                var coefficients = input.jpeg_read_coefficients();

                var channels = coefficients.Take(3).Select(JpegHelper.GetBuffer).ToArray();
                Encrypt(channels, data);

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

        private byte[] Dec(string inputPath, int len)
        {
            var input = new jpeg_decompress_struct();
            input.jpeg_stdio_src(new FileStream(inputPath, FileMode.Open));
            input.jpeg_read_header(false);
            var coefficients = input.jpeg_read_coefficients();

            var channels = coefficients.Take(3).Select(JpegHelper.GetBuffer).ToArray();
            return Decrypt(channels, len);
        }

        private void Encrypt(JBLOCK[][][] coefficients, byte[] data)
        {
            var l = JpegHelper.GetLength(coefficients);
            Console.Out.WriteLine($"Capacity = {l / 8}");

            Console.Out.WriteLine("Used = {0}", data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    var block = JpegHelper.GetBlock(coefficients, i * 8 + j);
                    block[0] = EncryptShort(block[0], BitHelper.GetBit(data[i], j));
                }
            }
        }

        private static short EncryptShort(short container, bool val)
        {
            return val ? (short) (container | 1) : (short) (container & (~1));
        }

        private byte[] Decrypt(JBLOCK[][][] coefficients, int length)
        {
            var res = new byte[length];

            for (var i = 0; i < length; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    var block = JpegHelper.GetBlock(coefficients, i * 8 + j);
                    BitHelper.SetBit(ref res[i], j, DecryptShort(block[0]));
                }
            }

            return res;
        }

        private bool DecryptShort(short container)
        {
            return (container & 1) == 1;
        }
    }
}