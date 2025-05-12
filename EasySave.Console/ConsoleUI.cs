using System;
using EasySave.Core.Services;
using EasySave.Localization;

namespace EasySave.Console
{
    public static class ConsoleUI
    {
        public static void ShowWelcomeMessage(LocalizationService localizationService)
        {
            System.Console.WriteLine(localizationService.GetLocalizedString("welcomeMessage"));
        }

        public static void ShowHelp(LocalizationService localizationService)
        {
            System.Console.WriteLine(localizationService.GetLocalizedString("commandLists"));
            System.Console.WriteLine("  - listjobs      : " + localizationService.GetLocalizedString("listJobsDescription"));
            System.Console.WriteLine("  - addjob        : " + localizationService.GetLocalizedString("addJobDescription"));
            System.Console.WriteLine("  - deletejob     : " + localizationService.GetLocalizedString("deleteJobDescription"));
            System.Console.WriteLine("  - runjob        : " + localizationService.GetLocalizedString("runJobDescription"));
            System.Console.WriteLine("  - language      : " + localizationService.GetLocalizedString("changeLanguageDescription"));
            System.Console.WriteLine("  - logtype      : " + localizationService.GetLocalizedString("changeLogFileTypeDescription"));
            System.Console.WriteLine("  - help          : " + localizationService.GetLocalizedString("helpDescription"));
            System.Console.WriteLine("  - exit          : " + localizationService.GetLocalizedString("exitDescription"));

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
