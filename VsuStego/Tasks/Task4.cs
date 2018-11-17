using System.IO;
using BitMiracle.LibJpeg.Classic;

namespace VsuStego.Tasks
{
    public class Task4 : ITask
    {
        public void Run()
        {
            var input = new jpeg_decompress_struct();
            input.jpeg_stdio_src(new FileStream("flowers.jpg", FileMode.Open));
            input.jpeg_read_header(false);
            var jpegReadCoefficients = input.jpeg_read_coefficients();

            var output = new jpeg_compress_struct();
            output.jpeg_stdio_dest(new FileStream("out.jpg", FileMode.Create));
         
            input.jpeg_copy_critical_parameters(output);

            output.jpeg_write_coefficients(jpegReadCoefficients);
            output.jpeg_finish_compress();


        }
    }
}