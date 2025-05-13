using System.Diagnostics;
using System.IO;
using System.Text;

namespace Cryptosoft
{
    class XOR
    {
        // static key but it's possible to read from a file
        private static string Key { get; } = "Ces1Kryp";

        private string Source { get; set; }

        private string Destination { get; set; }

        public XOR(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }

        public int StartEncrypt()
        {
            if (!File.Exists(Source))
            {
                return -2; // The file doesn't exist
            }

            //Stopwatch stopWatch = Stopwatch.StartNew();

            try
            {
                SaveToDestination(XOREncrypt(File.ReadAllText(Source, Encoding.UTF8)));
            }
            catch
            {
                return -3; // Encryption failed
            }

            //stopWatch.Stop();

            return 1; // (int)stopWatch.ElapsedMilliseconds;
        }

        private string XOREncrypt(string data)
        {
            var dataLen = data.Length;
            var keyLen = Key.Length;
            char[] output = new char[dataLen];

            for (var i = 0; i < dataLen; ++i)
            {
                output[i] = (char)(data[i] ^ Key[i % keyLen]);
            }

            return new string(output);
        }

        private void SaveToDestination(string encrypted)
        {
            new FileInfo(Destination).Directory.Create();
            File.WriteAllText(Destination, encrypted);
        }
    }
}
