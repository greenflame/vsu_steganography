using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VsuStego.Tasks
{
    public class Task3 : ITask
    {
        public void Run()
        {
            var salt = " ";
            var encryptedFileName = "__encrypted";

            //BruteForce(salt, encryptedFileName);
            //Decode(salt, "1557075050", encryptedFileName, "dectypted.jpg");
            BatchDecode(salt, "log.txt", encryptedFileName);
        }

        private readonly ReaderWriterLock _locker = new ReaderWriterLock();

        private void Log(string[] lines)
        {
            try
            {
                _locker.AcquireWriterLock(TimeSpan.FromSeconds(10));
                lines.ToList().ForEach(Console.WriteLine);
                File.AppendAllLines("log.txt", lines);
            }
            finally
            {
                _locker.ReleaseWriterLock();
            }
        }

        private void Log(string line)
        {
            Log(new[] {line});
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
                Decode(salt, keys[i], input, $"out\\{keys[i]}.jpg");
                Log($"Decoded: {i} of {keys.Count}");
            }
        }

        private void BruteForce(string salt, string input)
        {
            Log("Start pos:");
            var start = Convert.ToInt32(Console.ReadLine());
            Log("Step:");
            var step = Convert.ToInt32(Console.ReadLine());
            Log("Threads:");
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
                                Log($"RESULT: {i}");
                            }
                        }

                        Log($"{batchInd} processed");
                    }
                });
        }

        private bool Check(Aes aes, Stream input, byte[] Key, byte[] IV, byte[] tmpBuf)
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
                    Log("Jpeg detected");
                    return true;
                }

                if (tmpBuf[0] == 137 && tmpBuf[1] == 80 && tmpBuf[2] == 78)
                {
                    Log("Png detected");
                    return true;
                }

                if (tmpBuf[0] == 71 && tmpBuf[1] == 73 && tmpBuf[2] == 70)
                {
                    Log("Gif detected");
                    return true;
                }

                if (tmpBuf[0] == 60 && tmpBuf[1] == 115 && tmpBuf[2] == 118)
                {
                    Log("Svg detected");
                    return true;
                }

                if (tmpBuf[0] == 60 && tmpBuf[1] == 120 && tmpBuf[2] == 109)
                {
                    Log("Xml detected");
                    return true;
                }
            }

            return false;
        }
    }
}