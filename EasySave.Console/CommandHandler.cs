using System;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.Localization;

namespace EasySave.Console
{
    public class CommandHandler
    {
        private readonly BackupService _backupService;
        private readonly LocalizationService _localizationService;

        public CommandHandler(BackupService backupService, LocalizationService localizationService)
        {
            _backupService = backupService;
            _localizationService = localizationService;
        }

        public void Execute(string command)
        {
            switch (command)
            {
                case "listjobs":
                    _backupService.ListBackupJobs();
                    break;
                case "addjob":
                    AddJob();
                    break;
                case "deletejob":
                    DeleteJob();
                    break;
                case "runjob":
                    RunJob();
                    break;
                case "language":
                    ChangeLanguage();
                    break;
                case "help":
                    ConsoleUI.ShowHelp();
                    break;
                default:
                    ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("invalidCommand"));
                    break;
            }
        }

        private void AddJob()
        {
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("addJobMessage"));
            string name = ConsoleUI.GetUserInput("Nom du job : ");
            string sourcePath = ConsoleUI.GetUserInput("Chemin source : ");
            string targetPath = ConsoleUI.GetUserInput("Chemin cible : ");
            string typeInput = ConsoleUI.GetUserInput("Type de sauvegarde (Complète tapez 1 / Différentielle tapez 2) : ");

            BackupType backupType = typeInput switch
            {
                "1" => BackupType.Full,
                "2" => BackupType.Differential,
                _ => BackupType.Full
            };

            _backupService.CreateBackupJob(name, sourcePath, targetPath, backupType);
        }

        private void DeleteJob()
        {
            _backupService.ListBackupJobs();

            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("deleteJobMessage"));
            string input = ConsoleUI.GetUserInput("Entrez le numéro du job à supprimer : ");

            if (int.TryParse(input, out int jobNumber))
            {
                _backupService.DeleteBackupJobByIndex(jobNumber - 1);
            }
            else
            {
                ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("invalidJobNumber"));
            }
        }

        private void RunJob()
        {
            _backupService.ListBackupJobs();
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("runJobMessage"));
            string input = ConsoleUI.GetUserInput("Entrez le numéro du job à exécuter : ");

            if (int.TryParse(input, out int jobNumber))
            {
                _backupService.RunBackupJobByIndex(jobNumber - 1);
            }
            else
            {
                ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("invalidJobNumber"));
            }
        }

        private void ChangeLanguage()
        {
            ConsoleUI.ShowMessage("Choisissez une langue : 1 pour Français, 2 pour Anglais");
            string choice = ConsoleUI.GetUserInput("> ");
            string language = choice switch
            {
                "1" => "fr",
                "2" => "en",
                _ => "en"
            };

            _localizationService.SetLanguage(language);
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("languageChanged", language));
        }
    }
}
