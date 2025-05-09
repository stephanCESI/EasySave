using Newtonsoft.Json;
using System;
using System.IO;

namespace EasySave.Core.Utils
{
    public class AppSettings
    {
        public string DefaultLanguage { get; set; }
        public string LogDirectory { get; set; }
        public int MaxBackupJobs { get; set; }

        public static AppSettings Load()
        {
            try
            {
                // Récupérer le répertoire de base de l'application
                string basePath = AppContext.BaseDirectory;
                // Construire le chemin vers AppSettings.json
                var filePath = Path.Combine(basePath,"Utils", "AppSettings.json");

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<AppSettings>(jsonContent);
                }
                else
                {
                    Console.WriteLine($"Fichier de configuration '{filePath}' introuvable. Utilisation des paramètres par défaut.");
                    return new AppSettings
                    {
                        DefaultLanguage = "en",
                        LogDirectory = "Logs",
                        MaxBackupJobs = 5
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des paramètres : {ex.Message}");
                return new AppSettings
                {
                    DefaultLanguage = "en",
                    LogDirectory = "Logs",
                    MaxBackupJobs = 5
                };
            }
        }

    }
}
