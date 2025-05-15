using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace EasySave.Maui.Localizations
{
    public class LocalizationService
    {
        private Dictionary<string, Dictionary<string, string>> _localizations;
        private string _currentLanguage;
        private string _localizationsPath;

        public LocalizationService(string defaultLanguage = "en")
        {
            _currentLanguage = defaultLanguage;
            _localizations = new Dictionary<string, Dictionary<string, string>>();

            // Définir le chemin des ressources à partir du répertoire de base
            string basePath = AppContext.BaseDirectory;
            _localizationsPath = Path.Combine(basePath, "Localizations");

            LoadLocalizations();
        }

        public void LoadLocalizations()
        {
            try
            {
                var languages = new[] { "en", "fr" };

                foreach (var language in languages)
                {
                    var filePath = Path.Combine(_localizationsPath, $"{language}.json");

                    if (File.Exists(filePath))
                    {
                        var jsonContent = File.ReadAllText(filePath);
                        var localization = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                        _localizations[language] = localization;
                    }
                    else
                    {
                        Console.WriteLine($"Fichier de localisation '{filePath}' introuvable.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des localisations : {ex.Message}");
            }
        }

        public void SetLanguage(string language)
        {
            if (_localizations.ContainsKey(language))
            {
                _currentLanguage = language;
            }
            else
            {
                Console.WriteLine($"Langue '{language}' non trouvée. Utilisation de la langue par défaut.");
                _currentLanguage = "en";
            }
        }

        public string GetLocalizedString(string key, params object[] args)
        {
            if (_localizations.ContainsKey(_currentLanguage) && _localizations[_currentLanguage].ContainsKey(key))
            {
                var formatString = _localizations[_currentLanguage][key];
                return string.Format(formatString, args);
            }

            return key;
        }
    }

}
