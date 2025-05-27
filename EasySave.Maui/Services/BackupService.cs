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
using System.Text;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Alerts;
using System.Diagnostics;
using System.Collections.Concurrent;


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
        private static ConcurrentDictionary<string, bool> RemainingPriorityFiles = new();
        private static SemaphoreSlim LargeFileSemaphore = new(1, 1);


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
                System.Diagnostics.Debug.WriteLine(ex.Message);
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
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public void CreateBackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {
            try
            {
                if (_backupJobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var newJob = new BackupJob(name, sourcePath, targetPath, type);
                _backupJobs.Add(newJob);
                SaveJobsToFile();
                Toast.Make($"Le job {newJob.Name} a été ajouté avec succès.", ToastDuration.Short).Show();
            }

            catch (Exception ex)
            {
                Toast.Make(ex.Message, ToastDuration.Short).Show();
            }
        }

        public void DeleteBackupJobByIndex(int index)
        {
            try
            {
                if (index < 0 || index >= _backupJobs.Count)
                {
                    return;
                }

                var job = _backupJobs[index];
                _backupJobs.RemoveAt(index);
                SaveJobsToFile();
                Toast.Make($"Le job {job.Name} a été supprimé avec succès.", ToastDuration.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.Make(ex.Message, ToastDuration.Short).Show();
            }

        }

        public void DeleteBackupJobByName(string name)
        {
            try
            {
                var job = _backupJobs.FirstOrDefault(j => j.Name == name);
                if (job != null)
                {
                    _backupJobs.Remove(job);
                    SaveJobsToFile();
                    Toast.Make($"Le job {job.Name} a été supprimé avec succès.", ToastDuration.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.Make(ex.Message, ToastDuration.Short).Show();
            }

        }

        public void RunBackupJobByIndex(int index, bool IsCryptChecked)
        {
            try
            {
                if (index < 0 || index >= _backupJobs.Count)
                {
                    Toast.Make($"Le job avec n° {index} est inexistant", ToastDuration.Short).Show();
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
            catch (Exception ex)
            {
                Toast.Make(ex.Message, ToastDuration.Short).Show();
            }
        }

        public void RunBackupJob(BackupJob job, bool IsCryptChecked)
        {
            try
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
            catch (Exception ex)
            {
                Toast.Make(ex.Message, ToastDuration.Short).Show();
            }

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
                Toast.Make(ex.Message, ToastDuration.Short).Show();
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
            // var globalStopwatch = Stopwatch.StartNew(); 
            // ConcurrentDictionary<int, int> threadUsage = new ConcurrentDictionary<int, int>();
            // ConcurrentDictionary<int, long> threadFileSizes = new ConcurrentDictionary<int, long>();


            try
            {
                if (!Directory.Exists(job.SourcePath))
                    return;

                if (!Directory.Exists(job.TargetPath))
                    Directory.CreateDirectory(job.TargetPath);

                var settings = AppSettings.Load();
                List<string> businessSoftwares = settings.Softwares ?? new List<string>();

                if (IsBusinessSoftwareRunning(businessSoftwares))
                {
                    Toast.Make("Un logiciel métier est en cours d'exécution. Sauvegarde annulée.", ToastDuration.Short).Show();
                    return;
                }

                var encryptExtensions = settings.EncryptExtensions?.Select(e => e.ToLower()).ToList() ?? new List<string>();
                var cryptoService = new EncryptWithCryptoSoft();

                string[] files = Directory.GetFiles(job.SourcePath, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                int processedFiles = 0;

                var state = new BackupState(job.Name)
                {
                    SourceFilePath = job.SourcePath,
                    TargetFilePath = job.TargetPath,
                    State = "ACTIVE",
                    TotalFilesToCopy = totalFiles,
                    NbFilesLeftToDo = totalFiles,
                    Progression = 0
                };

                _stateManager.UpdateState(state);

                object lockObj = new object();

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 1)
                };

                var priorityExtensions = settings.PriorityExtensions?.Select(e => e.ToLower()).ToList() ?? new List<string>();

                long maxFileSizeBytes = 400 * 1024;
                if (settings.FileMaxSizes?.FirstOrDefault() is string maxSizeStr && long.TryParse(maxSizeStr, out long maxSizeKo))
                {
                    maxFileSizeBytes = maxSizeKo * 1024;
                }

                foreach (string file in files)
                {
                    if (priorityExtensions.Contains(Path.GetExtension(file).ToLower()))
                    {
                        RemainingPriorityFiles.TryAdd(file, true);
                    }
                }

                Parallel.ForEach(files, parallelOptions, file =>
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;

                    string extension = Path.GetExtension(file).ToLower();
                    bool isPriorityFile = priorityExtensions.Contains(extension);

                    while (!isPriorityFile && BackupController.RemainingPriorityFiles.Any(kvp => kvp.Value))
                    {
                        Thread.Sleep(100);
                    }

                    long fileSize = new FileInfo(file).Length;
                    bool isLargeFile = fileSize > maxFileSizeBytes;

                    if (isLargeFile)
                    {
                        BackupController.LargeFileSemaphore.Wait();
                    }

                    if (IsBusinessSoftwareRunning(businessSoftwares))
                    {
                        Toast.Make("Un logiciel métier est en cours d'exécution. Sauvegarde annulée.", ToastDuration.Short).Show();
                        lock (lockObj)
                        {
                            _logger.LogBackupAction(job.Name, job.SourcePath, job.TargetPath, fileSize, _timer.GetElapsedMilliseconds(), 0, true);
                        }

                        if (isLargeFile)
                        {
                            BackupController.LargeFileSemaphore.Release();
                        }

                        return;
                    }

                    string relativePath = Path.GetRelativePath(job.SourcePath, file);
                    string destinationFile = Path.Combine(job.TargetPath, relativePath);
                    bool shouldEncrypt = IsCryptChecked && encryptExtensions.Contains(extension);
                    bool sourceEncrypted = IsFileEncrypted(file);
                    double encryptionTime = 0;

                    try
                    {
                        bool filesAreIdentical = false;

                        if (job.Type == BackupType.Differential && File.Exists(destinationFile))
                        {
                            bool destEncrypted = IsFileEncrypted(destinationFile);

                            if (sourceEncrypted == destEncrypted)
                            {
                                filesAreIdentical = CompareFileHashes(file, destinationFile);
                            }

                            if (filesAreIdentical)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fichier inchangé : {file}");
                                lock (lockObj) processedFiles++;

                                if (isPriorityFile)
                                    BackupController.RemainingPriorityFiles[file] = false;

                                return;
                            }
                        }

                        _timer.Start();
                        _fileHelper.CopyFile(file, destinationFile);
                        _timer.Stop();

                        if (sourceEncrypted)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fichier déjà chiffré : {file}, aucune action de chiffrement.");
                        }
                        else if (shouldEncrypt)
                        {
                            try
                            {
                                var encryptionTimer = Stopwatch.StartNew();
                                bool success = cryptoService.EncryptFile(destinationFile, destinationFile);
                                encryptionTimer.Stop();

                                encryptionTime = success ? encryptionTimer.Elapsed.TotalMilliseconds : -1;
                            }
                            catch
                            {
                                encryptionTime = -1;
                            }
                        }

                        lock (lockObj)
                        {
                            _stateManager.UpdateState(state);
                            _logger.LogBackupAction(job.Name, job.SourcePath, job.TargetPath, fileSize, _timer.GetElapsedMilliseconds(), encryptionTime, false);
                        }

                        if (isPriorityFile)
                            BackupController.RemainingPriorityFiles[file] = false;
                    }
                    catch (Exception ex)
                    {
                        Toast.Make(ex.Message, ToastDuration.Short).Show();
                    }
                    finally
                    {
                        if (isLargeFile)
                        {
                            BackupController.LargeFileSemaphore.Release();
                        }
                    }
                });


                // globalStopwatch.Stop(); System.Diagnostics.Debug.WriteLine($"Sauvegarde terminée en {globalStopwatch.Elapsed.TotalSeconds:F2} secondes.");
                // System.Diagnostics.Debug.WriteLine($"Nombre de threads utilisés : {threadUsage.Count}");

                // foreach (var kvp in threadUsage.OrderBy(kvp => kvp.Key))
                // {
                // long sizeInBytes = threadFileSizes.TryGetValue(kvp.Key, out long totalSize) ? totalSize : 0;
                // string sizeFormatted = FormatBytes(sizeInBytes);
                // System.Diagnostics.Debug.WriteLine($"Thread {kvp.Key} a traité {kvp.Value} fichier(s), taille cumulée : {sizeFormatted}");
                // }

                Toast.Make($"Le job {job.Name} a été réalisé avec succès.", ToastDuration.Short).Show();
                state.State = "END";
                state.Progression = 100;
                _stateManager.UpdateState(state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
            }
        }

        /*
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
        */


        private bool IsBusinessSoftwareRunning(List<string> softwareNames)
        {
            try
            {
                foreach (var softwareName in softwareNames)
                {
                    var processes = Process.GetProcessesByName(softwareName);
                    if (processes.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Logiciel métier détecté : {softwareName}");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification des logiciels métiers : {ex.Message}");
                return false;
            }
        }

        private bool IsFileEncrypted(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[8];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);

                    if (bytesRead < buffer.Length)
                        return false;

                    string decrypted = XOREncrypt(Encoding.UTF8.GetString(buffer));

                    return decrypted.StartsWith("Ces1Kryp");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification du chiffrement du fichier {filePath} : {ex.Message}");
                return false;
            }
        }

        private string XOREncrypt(string data)
        {
            var dataLen = data.Length;
            var keyLen = "Ces1Kryp".Length;
            char[] output = new char[dataLen];

            for (var i = 0; i < dataLen; ++i)
            {
                output[i] = (char)(data[i] ^ "Ces1Kryp"[i % keyLen]);
            }

            return new string(output);
        }

    }


    public static class BackupController
    {
        // Stocke l'état de chaque fichier prioritaire (true = en attente, false = terminé)
        public static ConcurrentDictionary<string, bool> RemainingPriorityFiles { get; } = new();

        // Contrôle le nombre de fichiers "lourds" (> maxFileSize) copiés en même temps
        public static SemaphoreSlim LargeFileSemaphore { get; } = new(1, 1);
    }

}
