using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace EasySave.Localization
{
    public class LocalizationService
    {
        private Dictionary<string, Dictionary<string, string>> _localizations;
        private string _currentLanguage;
        private string _resourcesPath; // Déclaration de _resourcesPath ici

        public LocalizationService(string defaultLanguage = "en")
        {
            _currentLanguage = defaultLanguage;
            _localizations = new Dictionary<string, Dictionary<string, string>>();

            // Initialisation du chemin vers les ressources dans le constructeur
            string solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName;
            _resourcesPath = Path.Combine(solutionDirectory, "EasySave.Localization", "Resources");

            LoadLocalizations();
        }

        public void LoadLocalizations()
        {
            try
            {
                var languages = new[] { "en", "fr" }; // Langues disponibles

                // Si _resourcesPath se termine par un séparateur, l'enlever
                string normalizedPath = _resourcesPath.TrimEnd(Path.DirectorySeparatorChar);

                foreach (var language in languages)
                {
                    var filePath = Path.Combine(normalizedPath, $"{language}.json"); // Combine sans double séparateur

                    if (File.Exists(filePath))
                    {
                        var jsonContent = File.ReadAllText(filePath);
                        var localization = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                        _localizations[language] = localization;
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
                Console.WriteLine($"Langue changée en {language}");
            }
            else
            {
                Console.WriteLine($"Langue '{language}' non trouvée. Utilisation de la langue par défaut.");
                _currentLanguage = "en"; // Revenir à la langue par défaut (anglais) si la langue n'existe pas
            }
        }

        public string GetLocalizedString(string key, params object[] args)
        {
            if (_localizations.ContainsKey(_currentLanguage) && _localizations[_currentLanguage].ContainsKey(key))
            {
                var formatString = _localizations[_currentLanguage][key];
                return string.Format(formatString, args);
            }

            return key; // Si la clé n'existe pas, renvoyer la clé comme message
        }
    }
}
