using Newtonsoft.Json;
using System;
using System.IO;

namespace EasySave.Maui.Utils
{
    public class AppSettings
    {
        public string DefaultLanguage { get; set; }
        public string LogDirectory { get; set; }
        public string LogFileType { get; set; }
        public List<string> EncryptExtensions { get; set; }
        public List<string> Softwares { get; set; }
        public List<string> PriorityExtensions { get; set; }
        public List<string> FileMaxSizes { get; set; }

        public static AppSettings Load()
        {
            try
            {
                string basePath = AppContext.BaseDirectory;
                string projectRoot = Path.Combine(basePath, "..", "..", "..", "..", "..");
                string utilsPath = Path.Combine(projectRoot, "Utils", "AppSettings.json");
                string fullPath = Path.GetFullPath(utilsPath);

                if (File.Exists(fullPath))
                {
                    string jsonContent = File.ReadAllText(fullPath);
                    return JsonConvert.DeserializeObject<AppSettings>(jsonContent);
                }
                else
                {
                    Console.WriteLine($"Fichier de configuration '{fullPath}' introuvable. Utilisation des paramètres par défaut.");
                    return new AppSettings
                    {
                        DefaultLanguage = "en",
                        LogDirectory = "Logs",
                        LogFileType = "json",
                        EncryptExtensions = [],
                        Softwares = [],
                        PriorityExtensions = [],
                        FileMaxSizes = []
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
                    LogFileType = "json",
                    EncryptExtensions = [],
                    Softwares = [],
                    PriorityExtensions = [],
                    FileMaxSizes = []
                };
            }
        }

    }
}
