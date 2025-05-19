using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.Maui.Interfaces;
using EasySave.Maui.Models;
using EasySave.Maui.Utils;
using EasySave.Maui.Logging;
using EasySave.Maui.Localizations;
using System.Data;
using System.Security.Cryptography;

namespace EasySave.Maui.Services
{
    public class BackupService : IBackupService
    {
        private readonly List<BackupJob> _backupJobs;
        private readonly FileHelper _fileHelper;
        private readonly PerformanceTimer _timer;
        private readonly Logger _logger;
        private readonly LocalizationService _localizationService;
        private readonly string _logDirectory;
        private readonly StateManager _stateManager;
        private readonly string _backUpFilePath;

        public BackupService(Logger logger, LocalizationService localizationService)
        {
            _backupJobs = new List<BackupJob>();
            _fileHelper = new FileHelper();
            _timer = new PerformanceTimer();
            _logger = logger;
            _localizationService = localizationService;

            var settings = AppSettings.Load();
            _logDirectory = Path.Combine(AppContext.BaseDirectory, settings.LogDirectory);

            _stateManager = new StateManager(_logDirectory);


            _backUpFilePath = Path.Combine(_logDirectory, "backupJobs.json");

            LoadJobsFromFile();
        }

        public void SaveJobsToFile()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
                string jsonContent = JsonConvert.SerializeObject(_backupJobs, Formatting.Indented);
                File.WriteAllText(_backUpFilePath, jsonContent);

            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

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
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        public void CreateBackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {

            if (_backupJobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var newJob = new BackupJob(name, sourcePath, targetPath, type);
            _backupJobs.Add(newJob);
            SaveJobsToFile();
        }

        public void DeleteBackupJobByIndex(int index)
        {
            if (index < 0 || index >= _backupJobs.Count)
            {
                return;
            }

            var job = _backupJobs[index];
            _backupJobs.RemoveAt(index);
            SaveJobsToFile();
        }

        public void DeleteBackupJobByName(string name)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Name == name);
            if (job != null)
            {
                _backupJobs.Remove(job);
                SaveJobsToFile();
            }
        }

        public void RunBackupJobByIndex(int index, bool IsCryptChecked)
        {
            if (index < 0 || index >= _backupJobs.Count)
            {
                return;
            }

            var job = _backupJobs[index];
            job.IsActive = true;
            job.LastRun = DateTime.Now;
            UpdateState(job);

            PerformBackup(job, IsCryptChecked);
            job.IsActive = false;
            UpdateState(job);

            LoadJobsFromFile();
        }

        public void RunBackupJob(BackupJob job, bool IsCryptChecked)
        {
            if (job == null) return;

            job.IsActive = true;
            job.LastRun = DateTime.Now;

            PerformBackup(job, IsCryptChecked);
            UpdateState(job);

            job.IsActive = false;
            UpdateState(job);

            LoadJobsFromFile();
        }

        public void UpdateState(BackupJob newJob)
        {
            var existingState = _backupJobs.Find(s => s.Name == newJob.Name);

            SaveBackupJson();
        }

        private void SaveBackupJson()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_backupJobs, Formatting.Indented);
                File.WriteAllText(_backUpFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde de l'état : {ex.Message}");
            }
        }

        public void ListBackupJobs()
        {
            if (!_backupJobs.Any())
            {
                return;
            }

            int index = 1;

            foreach (var job in _backupJobs)
            {
                System.Console.WriteLine();
                index++;
            }
        }

        public IReadOnlyList<BackupJob> GetJobs()
        {
            return _backupJobs.AsReadOnly();
        }

        private static bool CompareFileHashes(string file1Path, string file2Path)
        {
            using var sha256 = SHA256.Create();
            byte[] hash1;
            byte[] hash2;

            using (var stream1 = File.OpenRead(file1Path))
                hash1 = sha256.ComputeHash(stream1);

            using (var stream2 = File.OpenRead(file2Path))
                hash2 = sha256.ComputeHash(stream2);

            return hash1.SequenceEqual(hash2);
        }

        private void PerformBackup(BackupJob job, bool IsCryptChecked)
        {
            try
            {
                if (!Directory.Exists(job.SourcePath))
                {
                    return;
                }

                if (!Directory.Exists(job.TargetPath))
                {
                    Directory.CreateDirectory(job.TargetPath);
                }

                var settings = AppSettings.Load();
                var encryptExtensions = settings.EncryptExtensions?.Select(e => e.ToLower()).ToList() ?? new List<string>();
                var cryptoService = new EncryptWithCryptoSoft();

                string[] files = Directory.GetFiles(job.SourcePath, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                long totalSize = files.Sum(file => new FileInfo(file).Length);
                int processedFiles = 0;
                long copiedSize = 0;

                var state = new BackupState(job.Name)
                {
                    SourceFilePath = job.SourcePath,
                    TargetFilePath = job.TargetPath,
                    State = "ACTIVE",
                    TotalFilesToCopy = totalFiles,
                    TotalFilesSize = totalSize,
                    NbFilesLeftToDo = totalFiles,
                    Progression = 0
                };

                _stateManager.UpdateState(state);

                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(job.SourcePath, file);
                    string destinationFile = Path.Combine(job.TargetPath, relativePath);
                    double transferTime = 0;
                    double encryptionTime = 0;
                    bool jobStopped = false;
                    long fileSize = new FileInfo(file).Length;

                    try
                    {
                        if (job.Type == BackupType.Differential &&
                            File.Exists(destinationFile) &&
                            CompareFileHashes(file, destinationFile) &&
                            new FileInfo(file).Length == new FileInfo(destinationFile).Length)
                        {
                            System.Console.WriteLine($"Fichier inchangé : {file}");
                        }
                        else
                        {
                            _timer.Start();
                            _fileHelper.CopyFile(file, destinationFile);
                            _timer.Stop();
                            transferTime = _timer.GetElapsedMilliseconds();
                            copiedSize += fileSize;
                            processedFiles++;

                            System.Console.WriteLine($"Fichier copié : {file} -> {destinationFile} (en {transferTime} ms)");
                        }

                        if (IsCryptChecked && encryptExtensions.Contains(Path.GetExtension(file).ToLower()))
                        {
                            try
                            {
                                var encryptionTimer = System.Diagnostics.Stopwatch.StartNew();
                                bool success = cryptoService.EncryptFile(destinationFile, destinationFile);
                                encryptionTimer.Stop();

                                if (success)
                                {
                                    encryptionTime = encryptionTimer.Elapsed.TotalMilliseconds;
                                    System.Console.WriteLine($"Fichier chiffré : {file} -> {destinationFile} (en {encryptionTime} ms)");
                                }
                                else
                                {
                                    encryptionTime = -1;
                                    System.Console.WriteLine($"Échec du chiffrement du fichier : {file}");
                                }
                            }
                            catch (Exception ex)
                            {
                                encryptionTime = -1;
                                System.Console.WriteLine($"Erreur lors du chiffrement du fichier {file} : {ex.Message}");
                            }
                        }

                        _logger.LogBackupAction(job.Name, file, destinationFile, fileSize, transferTime, encryptionTime, jobStopped);
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Erreur de traitement du fichier {file} : {ex.Message}");
                    }

                    double progression = (double)processedFiles / totalFiles * 100;
                    state.NbFilesLeftToDo = totalFiles - processedFiles;
                    state.Progression = progression;

                    _stateManager.UpdateState(state);
                }

                state.State = "END";
                state.Progression = 100;
                _stateManager.UpdateState(state);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
            }
        }


    }
}
