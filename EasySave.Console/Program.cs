using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.Localization;
using EasySave.Core.Utils;
using System;
using EasySave.Logging;

namespace EasySave.Console
{
    class Program
    {
        private static LocalizationService _localizationService;
        private static BackupService _backupService;
        private static CommandHandler _commandHandler;
        private static AppSettings _appSettings;

        static void Main(string[] args)
        {
            // Charger les paramètres depuis AppSettings.json
            _appSettings = AppSettings.Load();

            ILogWriter logWriter = new JsonLogWriter(_appSettings.LogDirectory);
            Logger logger = new Logger(logWriter);

            _localizationService = new LocalizationService(_appSettings.DefaultLanguage);
            _backupService = new BackupService(logger, _localizationService, _appSettings.MaxBackupJobs);
            _commandHandler = new CommandHandler(_backupService, _localizationService);

            _localizationService.LoadLocalizations();

            ConsoleUI.ShowWelcomeMessage(_localizationService);
            ConsoleUI.ShowHelp();

            string command;
            while (true)
            {
                command = ConsoleUI.GetUserInput("Entrez une commande : ");
                if (command == "exit")
                {
                    ConsoleUI.ShowMessage("Au revoir !");
                    break;
                }
                _commandHandler.Execute(command);
            }
        }
    }
}
