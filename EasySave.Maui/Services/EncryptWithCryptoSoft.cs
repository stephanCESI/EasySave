using System;
using System.Diagnostics;
using System.IO;

namespace EasySave.Maui.Services
{
    public class EncryptWithCryptoSoft
    {
        // Méthode pour chiffrer un fichier via CryptoSoft
        public bool EncryptFile(string sourceFile, string encryptedFile)
        {
            try
            {
                string basePath = AppContext.BaseDirectory;
                string projectRoot = Path.Combine(basePath, "..", "..", "..", "..", "..", "..");
                string utilsPath = Path.Combine(projectRoot, "CryptoSoft","bin", "Debug", "net8.0", "CryptoSoft.exe");
                string fullPath = Path.GetFullPath(utilsPath);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fullPath,
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
