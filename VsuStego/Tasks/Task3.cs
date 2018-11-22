using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VsuStego.Tasks
{
    public class Task3 : ITask
    {
        public void Run()
        {
            var salt = " ";
            var encryptedFileName = "__encrypted";

            BruteForce(salt, encryptedFileName);
            //Decode(salt, "1557075050", encryptedFileName, "dectypted.jpg");
            //BatchDecode(salt, "res2.txt", encryptedFileName);
        }

        private void Decode(string salt, string key, string input, string output)
        {
            using (var inputStream = new MemoryStream(File.ReadAllBytes(input)))
            using (var md5 = MD5.Create())
            using (var aes = Aes.Create())
            {
                aes.Padding = PaddingMode.Zeros;

                var iv = md5.ComputeHash(Encoding.ASCII.GetBytes(salt));
                var k = md5.ComputeHash(Encoding.ASCII.GetBytes(key));

                aes.Key = k;
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var csDecrypt = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                {
                    using (var ou = new FileStream(output, FileMode.Create))
                    {
                        csDecrypt.CopyTo(ou);
                    }
                }
            }
        }

        private void BatchDecode(string salt, string bruteForceOutput, string input)
        {
            var keys = File.ReadAllLines(bruteForceOutput).Where(s => s.Contains("RESULT: "))
                .Select(s => s.Substring("RESULT: ".Length))
                .ToList();

            for (var i = 0; i < keys.Count; i++)
            {
                Decode(salt, keys[i], input, $"out\\{keys[i]}.png");
                Console.Out.WriteLine($"Decoded: {i} of {keys.Count}");
            }
        }

        private static void BruteForce(string salt, string input)
        {
            Console.WriteLine("Start pos:");
            var start = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Step:");
            var step = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Threads:");
            var threads = Convert.ToInt32(Console.ReadLine());

            Parallel.ForEach(Enumerable.Range(start / step, int.MaxValue / step),
                new ParallelOptions {MaxDegreeOfParallelism = threads},
                batchInd =>
                {
                    var tmpBuf = new byte[16];

                    using (var inputStream = new MemoryStream(File.ReadAllBytes(input)))
                    using (var md5 = MD5.Create())
                    using (var aes = Aes.Create())
                    {
                        aes.Padding = PaddingMode.Zeros;

                        var iv = md5.ComputeHash(Encoding.ASCII.GetBytes(salt));

                        for (var i = batchInd * step; i < (batchInd + 1) * step; i++)
                        {
                            var key = md5.ComputeHash(Encoding.ASCII.GetBytes(i.ToString()));

                            if (Check(aes, inputStream, key, iv, tmpBuf))
                            {
                                Console.WriteLine($"RESULT: {i}");
                            }
                        }

                        Console.WriteLine($"{batchInd} processed");
                    }
                });
        }

        static bool Check(Aes aes, Stream input, byte[] Key, byte[] IV, byte[] tmpBuf)
        {
            aes.Key = Key;
            aes.IV = IV;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            input.Seek(0, SeekOrigin.Begin);

            var csDecrypt = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
            {
                csDecrypt.Read(tmpBuf, 0, 4);

                if (tmpBuf[0] == 255 && tmpBuf[1] == 216 && tmpBuf[2] == 255)
                {
                    Console.WriteLine("Jpeg detected");
                    return true;
                }

                if (tmpBuf[0] == 137 && tmpBuf[1] == 80 && tmpBuf[2] == 78)
                {
                    Console.WriteLine("Png detected");
                    return true;
                }

                if (tmpBuf[0] == 71 && tmpBuf[1] == 73 && tmpBuf[2] == 70)
                {
                    Console.WriteLine("Gif detected");
                    return true;
                }

                if (tmpBuf[0] == 60 && tmpBuf[1] == 115 && tmpBuf[2] == 118)
                {
                    Console.WriteLine("Svg detected");
                    return true;
                }

                if (tmpBuf[0] == 60 && tmpBuf[1] == 120 && tmpBuf[2] == 109)
                {
                    Console.WriteLine("Xml detected");
                    return true;
                }
            }

            return false;
        }
    }
}