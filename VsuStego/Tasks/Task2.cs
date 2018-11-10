using System;
using System.Drawing;
using VsuStego.Helpers;

namespace VsuStego.Tasks
{
    public class Task2 : ITask
    {
        public void Run()
        {
            var rect = new Rectangle(1500, 1300, 800, 600);

            Console.WriteLine("Encoding");
            Encode("flowers.jpg", "encoded.jpg", rect);

            Console.WriteLine("Decoding");
            Decode("encoded.jpg", "decoded.jpg", rect);
        }

        private static void Encode(string input, string output, Rectangle rect)
        {
            var originalImage = new Bitmap(Image.FromFile(input));
            var subImage = originalImage.Clone(rect, originalImage.PixelFormat);

            using (var g = Graphics.FromImage(originalImage))
            {
                g.FillRectangle(new SolidBrush(Color.Black), rect);
            }

            var codec = new LangelaarCodec(originalImage, new Size(8, 8), 100);

            subImage.SaveJpeg(codec, 10);
            
            Console.WriteLine($"Used {codec.Position} of {codec.Length}");

            originalImage.SaveJpeg(output, 100);
        }

        private static void Decode(string input, string output, Rectangle rect)
        {
            var encodedImage = new Bitmap(Image.FromFile(input));
            var codec = new LangelaarCodec(encodedImage, new Size(8, 8), 10);

            var subImage = Image.FromStream(codec);

            using (var g = Graphics.FromImage(encodedImage))
            {
                g.DrawImage(subImage, rect);
            }

            encodedImage.SaveJpeg(output, 100);
        }
    }
}