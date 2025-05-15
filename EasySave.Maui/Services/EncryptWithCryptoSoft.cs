using System;
using System.Diagnostics;
using System.IO;

namespace EasySave.Maui.Services
{
    public class EncryptWithCryptoSoft
    {
        private readonly string _cryptoSoftPath;

        public EncryptWithCryptoSoft(string cryptoSoftPath = "cryptosoft.exe")
        {
            _cryptoSoftPath = cryptoSoftPath;
        }

        // Méthode pour chiffrer un fichier via CryptoSoft
        public bool EncryptFile(string sourceFile, string encryptedFile)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _cryptoSoftPath,
                        Arguments = $"source \"{sourceFile}\" destination \"{encryptedFile}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 1)
                {
                    Console.WriteLine($"Chiffrement réussi : {sourceFile} -> {encryptedFile}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Erreur de chiffrement ({process.ExitCode}): {output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors du chiffrement : {ex.Message}");
                return false;
            }
        }
    }
}
