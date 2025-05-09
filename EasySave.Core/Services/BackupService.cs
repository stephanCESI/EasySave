using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Utils;
using EasySave.Logging;
using EasySave.Localization;

namespace EasySave.Core.Services
{
    public class BackupService : IBackupService
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly FileHelper _fileHelper;
        private readonly PerformanceTimer _timer;
        private readonly Logger _logger;
        private readonly LocalizationService _localizationService;
        private readonly int _maxBackupJobs;
        private readonly string _logDirectory;
        private readonly string _backupjobs;

        public BackupService(Logger logger, LocalizationService localizationService, int maxBackupJobs)
        {
            _backupJobs = new List<BackupJob>();
            _fileHelper = new FileHelper();
            _timer = new PerformanceTimer();
            _logger = logger;
            _localizationService = localizationService;
            _maxBackupJobs = maxBackupJobs;

            // Charger les paramètres depuis AppSettings.json
            var settings = AppSettings.Load();

            _logDirectory = settings.LogDirectory;

            LoadJobsFromFile();  // Charger les jobs au démarrage
        }


        // Sauvegarder les jobs dans un fichier JSON
        public void SaveJobsToFile()
        {
            try
            {
                // Chemin complet du fichier dans le répertoire des logs
                string filePath = Path.Combine(_logDirectory, "backupJobs.json");

                // Créer le répertoire s'il n'existe pas
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // Sérialiser la liste des jobs en JSON avec un format lisible
                string jsonContent = JsonConvert.SerializeObject(_backupJobs, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);

                System.Console.WriteLine(_localizationService.GetLocalizedString("jobsSaved", filePath));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("addJobError")+ ex.Message);
            }
        }

        // Charger les jobs depuis un fichier JSON
        public void LoadJobsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_logDirectory, "backupJobs.json");

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var loadedJobs = JsonConvert.DeserializeObject<List<BackupJob>>(jsonContent);

                    if (loadedJobs != null)
                    {
                        _backupJobs.Clear();
                        _backupJobs.AddRange(loadedJobs);
                    }
                }
                else
                {
                    System.Console.WriteLine(_localizationService.GetLocalizedString("noJobsFoundNewFile"));
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("errorLoadJobs") + ex.Message);
            }
        }

        public void CreateBackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {
            if (_backupJobs.Count >= _maxBackupJobs)
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("errorMaxJobsReached"));
                return;
            }

            if (_backupJobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("errorJobAlreadyExists", name));
                return;
            }

            var newJob = new BackupJob(name, sourcePath, targetPath, type);
            _backupJobs.Add(newJob);
            SaveJobsToFile();
            System.Console.WriteLine(_localizationService.GetLocalizedString("jobCreated", name));
        }

        public void DeleteBackupJobByIndex(int index)
        {
            if (index < 0 || index >= _backupJobs.Count)
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("errorJobIndexOutOfRange"));
                return;
            }

            var job = _backupJobs[index];
            _backupJobs.RemoveAt(index);
            SaveJobsToFile();
            System.Console.WriteLine(_localizationService.GetLocalizedString("jobDeleted", job.Name));
        }

        public void RunBackupJobByIndex(int index)
        {
            if (index < 0 || index >= _backupJobs.Count)
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("errorJobIndexOutOfRange"));
                return;
            }

            var job = _backupJobs[index];
            System.Console.WriteLine(_localizationService.GetLocalizedString("backupStarted", job.Name));
            job.IsActive = true;
            job.LastRun = DateTime.Now;

            PerformBackup(job);
            job.IsActive = false;
            System.Console.WriteLine(_localizationService.GetLocalizedString("backupCompleted", job.Name));
        }

        public void ListBackupJobs()
        {
            if (!_backupJobs.Any())
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("noJobsFound"));
                return;
            }

            int index = 1;

            foreach (var job in _backupJobs)
            {
                System.Console.WriteLine($"{index}. {_localizationService.GetLocalizedString("jobListItem", job.Name, job.SourcePath, job.TargetPath, job.Type, job.IsActive)}");
                System.Console.WriteLine();
                index++;
            }
        }


        private void PerformBackup(BackupJob job)
        {
            try
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("backupStartedInfo", job.Name));

                // Vérifier si les répertoires source et cible existent
                if (!Directory.Exists(job.SourcePath))
                {
                    System.Console.WriteLine(_localizationService.GetLocalizedString("errorSourceDirectoryNotFound", job.SourcePath));
                    return;
                }
                if (!Directory.Exists(job.TargetPath))
                {
                    Directory.CreateDirectory(job.TargetPath);
                    System.Console.WriteLine(_localizationService.GetLocalizedString("infoTargetDirectoryCreated", job.TargetPath));
                }

                string[] files = Directory.GetFiles(job.SourcePath, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                long totalSize = files.Sum(file => new FileInfo(file).Length);
                int processedFiles = 0;
                long copiedSize = 0;

                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(job.SourcePath, file);
                    string destinationFile = Path.Combine(job.TargetPath, relativePath);

                    // Sauvegarde différentielle : ignorer les fichiers déjà à jour
                    if (job.Type == BackupType.Differential &&
                        File.Exists(destinationFile) &&
                        File.GetLastWriteTime(file) <= File.GetLastWriteTime(destinationFile))
                    {
                        continue;
                    }

                    try
                    {
                        _timer.Start();
                        _fileHelper.CopyFile(file, destinationFile);
                        _timer.Stop();

                        double transferTime = _timer.GetElapsedMilliseconds();
                        long fileSize = new FileInfo(file).Length;
                        copiedSize += fileSize;
                        processedFiles++;

                        // Loguer chaque fichier copié
                        _logger.LogBackupAction(job.Name, file, destinationFile, fileSize, transferTime);

                        System.Console.WriteLine(_localizationService.GetLocalizedString("fileBackedUp", file, destinationFile, fileSize, transferTime));
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(_localizationService.GetLocalizedString("errorFileCopy", file, ex.Message));
                    }

                    // Afficher la progression
                    double progression = (double)processedFiles / totalFiles * 100;
                    System.Console.WriteLine(_localizationService.GetLocalizedString("progression", processedFiles, totalFiles, progression));
                }

                System.Console.WriteLine(_localizationService.GetLocalizedString("backupCompletedInfo", job.Name, processedFiles, totalFiles, copiedSize));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(_localizationService.GetLocalizedString("errorBackupFailed", ex.Message));
            }
        }
    }
}
