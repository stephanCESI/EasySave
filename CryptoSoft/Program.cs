using System;

namespace Cryptosoft
{
    class Program
    {
        static int Main(string[] args)
        {
            string source;
            string destination;

            try
            {
                // Lancer l'application console : Cryptosoft source fichier_source destination fichier destination
                int s = Array.IndexOf(args, "source");
                source = args[s + 1];
                int d = Array.IndexOf(args, "destination");
                destination = args[d + 1];
            }
            catch
            {
                return -1;
                // Erreur - Arguments "source" ou "destination" pas trouvés
            }

            return new XOR(source, destination).StartEncrypt();
        }
    }
}
