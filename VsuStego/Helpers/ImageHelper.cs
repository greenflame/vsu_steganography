using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace VsuStego.Helpers
{
    public static class ImageHelper
    {
        public static void SaveJpeg(this Image image, string path, int quality)
        {
            image.Save(path, GetEncoder(ImageFormat.Jpeg), GetEncoderParameters(quality));
        }

        public static void SaveJpeg(this Image image, Stream stream, int quality)
        {
            image.Save(stream, GetEncoder(ImageFormat.Jpeg), GetEncoderParameters(quality));
        }

        private static EncoderParameters GetEncoderParameters(int quality)
        {
            var parameters = new EncoderParameters
            {
                Param = new[]
                {
                    new EncoderParameter(Encoder.Quality, quality),
                }
            };
            return parameters;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        public static Bitmap Hash(Bitmap image, Size size)
        {
            var resized = Resize(image, size);
            var bw = ConvertToBlackAndWhite(resized, GetMeanBrightness(resized));
            return bw.Clone(new Rectangle(0, 0, bw.Width, bw.Height), PixelFormat.Format1bppIndexed);
        }

        private static Bitmap ConvertToBlackAndWhite(Bitmap image, int brightnessThreshold)
        {
            var bw = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);

            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                {
                    var pixel = image.GetPixel(i, j);
                    bw.SetPixel(i, j, GetBrightness(pixel) > brightnessThreshold ? Color.White : Color.Black);
                }
            }

            return bw;
        }

        private static int GetMeanBrightness(Bitmap image)
        {
            var result = 0;

            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                {
                    var pixel = image.GetPixel(i, j);
                    result += GetBrightness(pixel);
                }
            }

            return result / (image.Width * image.Height);
        }

        private static Bitmap Resize(Image image, Size size)
        {
            var destRect = new Rectangle(new Point(0, 0), size);
            var destImage = new Bitmap(size.Width, size.Height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Bitmap Validate(Bitmap signedImage, Bitmap oldHash, Bitmap newHash)
        {
            var diffMask = DiffMask(oldHash, newHash);

            using (var g = Graphics.FromImage(signedImage))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(diffMask, 0, 0, signedImage.Width, signedImage.Height);
            }

            return signedImage;
        }

        private static Bitmap DiffMask(Bitmap bmp1, Bitmap bmp2)
        {
            var result = new Bitmap(bmp1.Width, bmp1.Height, PixelFormat.Format32bppArgb);

            for (var i = 0; i < bmp1.Width; i++)
            {
                for (var j = 0; j < bmp1.Height; j++)
                {
                    var c = bmp1.GetPixel(i, j).ToArgb() == bmp2.GetPixel(i, j).ToArgb() ? Color.FromArgb(50, 0, 255, 0) : Color.FromArgb(50, 255, 0, 0);
                    result.SetPixel(i, j, c);
                }
            }

            return result;
        }

        public static int GetBrightness(Color color) => color.R + color.G + color.B;

        public static int MaxBrightness => byte.MaxValue * 3;
    }
}