using System;
using EasySave.Localization;

namespace EasySave.Console
{
    public static class ConsoleUI
    {
        public static void ShowWelcomeMessage(LocalizationService localizationService)
        {
            System.Console.WriteLine(localizationService.GetLocalizedString("welcomeMessage"));
        }

        public static void ShowHelp()
        {
            System.Console.WriteLine("Liste des commandes disponibles :");
            System.Console.WriteLine("  - listjobs      : Liste les jobs de sauvegarde");
            System.Console.WriteLine("  - addjob        : Ajouter un job de sauvegarde");
            System.Console.WriteLine("  - deletejob     : Supprimer un job de sauvegarde");
            System.Console.WriteLine("  - runjob        : Exécuter un job de sauvegarde");
            System.Console.WriteLine("  - language      : Changer la langue");
            System.Console.WriteLine("  - help          : Afficher l'aide");
            System.Console.WriteLine("  - exit          : Quitter l'application");
        }

        public static void ShowMessage(string message)
        {
            System.Console.WriteLine(message);
        }

        public static string GetUserInput(string prompt)
        {
            System.Console.Write(prompt);
            return System.Console.ReadLine();
        }
    }
}
