namespace EasySave.Localization
{
    public static class LanguageHelper
    {
        public static string[] GetAvailableLanguages()
        {
            return new[] { "en", "fr" }; // Liste des langues disponibles
        }

        public static string GetCurrentLanguage(LocalizationService localizationService)
        {
            return localizationService.GetLocalizedString("currentLanguage"); // Utilise une clé pour récupérer la langue actuelle
        }
    }
}
