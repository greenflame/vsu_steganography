using System;
using System.Drawing;
using System.IO;
using System.Linq;
using VsuStego.Helpers;

namespace VsuStego
{
    public class LangelaarCodec : Stream
    {
        public Bitmap Image { get; }

        public Size BlockSize { get; } = new Size(8 ,8);

        public int PixelDistance { get; } = 10;

        public int BlockDistance => PixelDistance * BlockLength;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => Image.Width / BlockSize.Width * (Image.Height / BlockSize.Height) / 8;

        public int BlockLength => BlockSize.Width * BlockSize.Height;

        public override long Position { get; set; }

        public LangelaarCodec(Bitmap image, Size blockSize, int pixelDistance)
        {
            Image = image;
            BlockSize = blockSize;
            PixelDistance = pixelDistance;
        }

        public LangelaarCodec(Bitmap image)
        {
            Image = image;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.Begin)
            {
                throw new NotSupportedException();
            }

            Position = offset;
            return offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > Length - Position)
            {
                throw new IndexOutOfRangeException();
            }

            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    BitHelper.SetBit(ref buffer[offset + i], j, ReadBit((int) Position * 8 + j));
                }
                Position++;
            }

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > Length - Position)
            {
                throw new IndexOutOfRangeException();
            }

            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    WriteBit((int) Position * 8 + j, BitHelper.GetBit(buffer[offset + i], j));
                }
                Position++;
            }
        }

        private bool ReadBit(int block)
        {
            var indexes = Enumerable.Range(0, BlockLength).ToLookup(GetGroup).Select(i => i.ToList()).ToList();
            var sum = indexes.Select(g => g.Select(p => GetPixel(block, p)).Select(ImageHelper.GetBrightness).Sum()).ToList();

            return sum[0] < sum[1];
        }

        private void WriteBit(int block, bool value)
        {
            var indexes = Enumerable.Range(0, BlockLength).ToLookup(GetGroup).Select(i => i.ToList()).ToList();
            var sum = indexes.Select(g => g.Select(p => GetPixel(block, p)).Select(ImageHelper.GetBrightness).Sum()).ToList();
            var max = indexes.Select(g => g.Count * ImageHelper.MaxBrightness).ToList();
            var mid = sum.Sum() / sum.Count;
            var target = indexes.Select(_ => 0).ToList();

            if (value)
            {
                target[0] = MathHelper.FitRange(mid - BlockDistance / 2, 0, sum[0]);
                target[1] = MathHelper.FitRange(mid + BlockDistance / 2, sum[1], max[1]);
            }
            else
            {
                target[0] = MathHelper.FitRange(mid + BlockDistance / 2, sum[0], max[0]);
                target[1] = MathHelper.FitRange(mid + BlockDistance / 2, 0, sum[1]);
            }

            for (var i = 0; i < BlockLength; i++)
            {
                var color = GetPixel(block, i);
                var g = GetGroup(i);

                if (target[g] < sum[g])
                {
                    var k = (double) target[g] / sum[g];

                    color = Color.FromArgb(
                        (byte) (color.R * k),
                        (byte) (color.G * k),
                        (byte) (color.B * k));
                }
                else
                {
                    var k = (double) (max[g] - target[g]) / (max[g] - sum[g]);

                    color = Color.FromArgb(
                        255 - (byte) ((255 - color.R) * k),
                        255 - (byte) ((255 - color.G) * k),
                        255 - (byte) ((255 - color.B) * k));
                }

                SetPixel(block, i, color);
            }

            var g0Sr = indexes[0].Select(i => GetPixel(block, i)).Select(p => p.R + p.G + p.B).Sum();
            var g1Sr = indexes[1].Select(i => GetPixel(block, i)).Select(p => p.R + p.G + p.B).Sum();

            if (value)
            {
                if (g0Sr > g1Sr) throw new Exception();
            }
            else
            {
                if (g0Sr < g1Sr) throw new Exception();
            }
        }

        private int GetGroup(int pixel) => (pixel % BlockSize.Width + pixel / BlockSize.Width) % 2;
        //private int GetGroup(int pixel) => pixel < BlockLength / 2 ? 0 : 1;

        private void SetPixel(int block, int pixel, Color color) =>
            Image.SetPixel(GetX(block, pixel), GetY(block, pixel), color);

        private Color GetPixel(int block, int pixel) => Image.GetPixel(GetX(block, pixel), GetY(block, pixel));

        private int GetY(int block, int pixel) =>
            block / (Image.Width / BlockSize.Width) * BlockSize.Width + pixel / BlockSize.Width;

        private int GetX(int block, int pixel) =>
            block % (Image.Width / BlockSize.Width) * BlockSize.Width + pixel % BlockSize.Width;
    }
}