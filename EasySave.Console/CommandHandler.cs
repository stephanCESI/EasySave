using System;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.Core.Utils;
using EasySave.Localization;
using EasySave.Logging;
using Newtonsoft.Json;

namespace EasySave.Console
{
    public class CommandHandler
    {
        private readonly BackupService _backupService;
        private readonly LocalizationService _localizationService;
        private Logger _logger;  // Propriété pour le logger

        public CommandHandler(BackupService backupService, LocalizationService localizationService)
        {
            _backupService = backupService;
            _localizationService = localizationService;
            AppSettings settings = AppSettings.Load();
            _logger = new Logger(settings.LogDirectory, settings.LogFileType);
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
                case "logtype":
                    ChangeLogFileType();
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

        private void ChangeLogFileType()
        {
            AppSettings settings = AppSettings.Load();

            // Afficher le type actuel
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("actualLogTypeFile", settings.LogFileType));

            // Demander le nouveau type
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("choseLogTypeFile"));
            string choice = ConsoleUI.GetUserInput("> ");
            string filetype = choice switch
            {
                "1" => "xml",
                "2" => "json",
                _ => null
            };

            // Vérifier si le choix est valide
            if (filetype == null)
            {
                ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("errorLogTypeFileChanged", settings.LogFileType));
                return;
            }

            // Mettre à jour le type de fichier de log
            settings.LogFileType = filetype;
            SaveSettings(settings);

            // Recharger le logger avec le nouveau type
            _logger = new Logger(settings.LogDirectory, settings.LogFileType);

            // Confirmer le changement
            ConsoleUI.ShowMessage(_localizationService.GetLocalizedString("logTypeFileChanged", filetype));
        }




        private void SaveSettings(AppSettings settings)
        {
            try
            {
                string basePath = AppContext.BaseDirectory;
                string filePath = Path.Combine(basePath, "Utils", "AppSettings.json");

                // Sauvegarder les paramètres dans le fichier JSON
                string jsonContent = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Erreur lors de la sauvegarde des paramètres : {ex.Message}");
            }
        }

    }
}
