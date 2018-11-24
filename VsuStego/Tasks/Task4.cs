using System;
using System.Linq;
using System.Text;

namespace VsuStego.Tasks
{
    public class Task4 : ITask
    {
        public void Run()
        {
            const int bytesPerGroup = 2;

            int[][] groups =
            {
                new[] // 28
                {
                    0, 1, 2, 3, 4, 5, 6,
                    8, 9, 10, 11, 12, 13,
                    16, 17, 18, 19, 20,
                    32, 33, 34, 35,
                    40, 41, 42,
                    48, 49,
                    56
                }
            };


            var highCapacityJpegCodec = new HighCapacityJpegCodec(bytesPerGroup, groups);

            var str = string.Join(" ", Enumerable.Range(0, 100).Select(_ => "Hello, world!"));

            highCapacityJpegCodec.EncodeJpeg("flowers.jpg", "out.jpg", Encoding.ASCII.GetBytes(str));

            Console.Out.WriteLine(Encoding.ASCII.GetString(highCapacityJpegCodec.DecodeJpeg("out.jpg", str.Length)));
        }
    }
}