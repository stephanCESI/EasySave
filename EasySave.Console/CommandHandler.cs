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
                    ConsoleUI.ShowHelp(_localizationService);
                    break;
                default:
                    ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("invalidCommand"));
                    break;
            }
        }

        private void AddJob()
        {
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("addJobMessage"));
            string name = ConsoleUI.GetUserInput(_localizationService.GetLocalizedString("jobName"));
            string sourcePath = ConsoleUI.GetUserInput(_localizationService.GetLocalizedString("sourcePath")).Trim('\"');
            string targetPath = ConsoleUI.GetUserInput(_localizationService.GetLocalizedString("targetPath")).Trim('\"');
            string typeInput = ConsoleUI.GetUserInput(_localizationService.GetLocalizedString("typeSave"));

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
            string input = ConsoleUI.GetUserInput(_localizationService.GetLocalizedString("deleteJobNumber"));

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
            string input = ConsoleUI.GetUserInput(_localizationService.GetLocalizedString("runJobNumber"));

            // Vérifier si l'entrée est un nombre simple
            if (int.TryParse(input, out int singleJobNumber))
            {
                _backupService.RunBackupJobByIndex(singleJobNumber - 1);
                return;
            }

            // Vérifier si l'entrée est une plage au format x-y
            if (input.Contains("-"))
            {
                string[] range = input.Split('-');
                if (range.Length == 2 &&
                    int.TryParse(range[0], out int start) &&
                    int.TryParse(range[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        _backupService.RunBackupJobByIndex(i - 1);
                    }
                    return;
                }
            }

            // Vérifier si l'entrée est une liste au format x;y
            if (input.Contains(";"))
            {
                string[] jobs = input.Split(';');
                foreach (string job in jobs)
                {
                    if (int.TryParse(job, out int jobNumber))
                    {
                        _backupService.RunBackupJobByIndex(jobNumber - 1);
                    }
                    else
                    {
                        ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("invalidJobNumber"));
                        return;
                    }
                }
                return;
            }

            // Message d'erreur si l'entrée est incorrecte
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("invalidJobNumber"));
        }


        private void ChangeLanguage()
        {
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("choseLanguage"));
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
