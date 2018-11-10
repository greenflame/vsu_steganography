using System.Drawing;
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
    }
}