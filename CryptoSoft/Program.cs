using System;
using System.Threading;

namespace Cryptosoft
{
    class Program
    {
        private static Mutex mutex = null;
        private const string MutexName = "CryptosoftMutex";

        static int Main(string[] args)
        {
            
            mutex = new Mutex(false, MutexName);
            if (!mutex.WaitOne(0, false))
            {
                Console.WriteLine("L'application est déjà en cours d'exécution.");
                return -1;
            }

            string source;
            string destination;

            try
            {
                try
                {
                    int s = Array.IndexOf(args, "source");
                    int d = Array.IndexOf(args, "destination");

                    if (s == -1 || s + 1 >= args.Length || d == -1 || d + 1 >= args.Length)
                    {
                        Console.WriteLine("Erreur : les arguments 'source' ou 'destination' sont manquants ou incorrects.");
                        return -1;
                    }

                    source = args[s + 1];
                    destination = args[d + 1];
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du traitement des arguments : {ex.Message}");
                    return -1;
                }

                return new XOR(source, destination).StartEncrypt();
            }
            finally
            {
                

                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }


        }
    }
}
