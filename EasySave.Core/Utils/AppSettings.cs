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


        // Charger les paramètres depuis AppSettings.json
        public static AppSettings Load()
        {
            try
            {

                string solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName;
                var filePath = Path.Combine(solutionDirectory, "EasySave.Core", "Utils", "AppSettings.json");

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<AppSettings>(jsonContent);
                }
                else
                {
                    System.Console.WriteLine($"Fichier de configuration '{filePath}' introuvable. Utilisation des paramètres par défaut.");
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
                System.Console.WriteLine($"Erreur lors du chargement des paramètres : {ex.Message}");
                // Valeurs par défaut en cas d'erreur
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
