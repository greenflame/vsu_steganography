using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using VsuStego.Helpers;

namespace VsuStego.Tasks
{
    public class Task1 : ITask
    {
        public void Run()
        {
            //Console.WriteLine("Signing");
            //Encode("flowers.jpg", "signed.jpg");

            Console.WriteLine("Checking");
            Decode("signed.jpg", "validated.jpg");
        }

        private static void Encode(string input, string output)
        {
            var originalImage = new Bitmap(Image.FromFile(input));
            var hash = ImageHelper.Hash(originalImage, new Size(100, 100));

            var codec = new LangelaarCodec(originalImage, new Size(8, 8), 10);

            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                ms.SetLength(2000);
                hash.Save(ms, ImageFormat.Png);
                buffer = ms.GetBuffer();
            }

            for (var i = 0; i < 8; i++)
            {
                codec.Write(buffer, 0, buffer.Length);
            }

            originalImage.SaveJpeg(output, 100);
        }

        private static void Decode(string input, string output)
        {
            var signedImage = new Bitmap(Image.FromFile(input));
            var codec = new LangelaarCodec(signedImage, new Size(8, 8), 10);

            var buffers = new List<byte[]>();

            for (var i = 0; i < 8; i++)
            {
                var b = new byte[2000];
                codec.Read(b, 0, b.Length);
                buffers.Add(b);
            }

            var buffer = BitHelper.Merge(buffers);

            Bitmap oldHash;

            using (var ms = new MemoryStream(buffer))
            {
                oldHash = new Bitmap(Image.FromStream(ms));
            }

            var newHash = ImageHelper.Hash(signedImage, new Size(100, 100));

            var validated = ImageHelper.Validate(signedImage, oldHash, newHash);

            validated.SaveJpeg(output, 100);
        }
    }
}