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

        public async Task RunBackupJobAsync(BackupJob job, bool isCryptChecked, CancellationToken cancellationToken, IProgress<double> progressReporter = null)
        {
            if (job == null) { System.Diagnostics.Debug.WriteLine("RunBackupJobAsync: job est null."); return; }

            if (job.IsActive)
            {
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make($"Job '{job.Name}' déjà en cours.", ToastDuration.Short).Show());
                return;
            }

            var settings = AppSettings.Load();
            if (IsBusinessSoftwareRunning(settings.Softwares ?? new List<string>()))
            {
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make("Logiciel métier actif. Lancement annulé.", ToastDuration.Long).Show());
                _logger.LogBackupAction(job.Name, "N/A", "N/A", 0, 0, 0, true);
                return;
            }

            job.IsUserPaused = false;
            job.IsSystemPaused = false;
            job.PauseSignal.Set();
            job.IsActive = true;
            job.LastRun = DateTime.Now;
            job.Progress = 0;
            progressReporter?.Report(0);

            UpdateRealTimeStateForJob(job, null, null, 0, 0, job.GetCurrentStatusDisplay());
            SaveJobsToFile();

            try
            {
                await Task.Run(() => PerformBackupInternal(job, isCryptChecked, cancellationToken, progressReporter), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Job '{job.Name}' annulé via CancellationToken.");
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make($"Job '{job.Name}' annulé.", ToastDuration.Short).Show());
                _logger.LogBackupAction(job.Name, "N/A", "N/A", 0, 0, 0, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur majeure Job '{job.Name}': {ex.Message} \nStackTrace: {ex.StackTrace}");
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make($"Erreur Job '{job.Name}': {ex.Message}", ToastDuration.Long).Show());
                _logger.LogBackupAction(job.Name, "N/A", "N/A", 0, 0, 0, true);
            }
            finally
            {
                job.NotifyCompletionOrStop();
                progressReporter?.Report(job.Progress / 100.0);
                UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
                SaveJobsToFile();
                ClearPriorityFilesForJob(job);
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
                File.WriteAllTextAsync(_backUpFilePath, jsonContent);
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

        private void UpdateRealTimeStateForJob(BackupJob job, string currentFileSource, string currentFileDest, int totalFiles, int filesDone, string explicitStatus = null)
        {
            if (job == null) return; // Sécurité
            var stateEntry = new BackupState(job.Name)
            {
                SourceFilePath = currentFileSource,
                TargetFilePath = currentFileDest,
                State = explicitStatus ?? job.GetCurrentStatusDisplay(),
                TotalFilesToCopy = totalFiles,
                NbFilesLeftToDo = totalFiles - filesDone,
                Progression = job.Progress
            };
            _stateManager.UpdateState(stateEntry);
        }


        private int GetTotalFilesForJob(BackupJob job)
        {
            try { if (job != null && !string.IsNullOrEmpty(job.SourcePath) && Directory.Exists(job.SourcePath)) return Directory.GetFiles(job.SourcePath, "*.*", SearchOption.AllDirectories).Length; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Erreur GetTotalFilesForJob pour {job?.Name}: {ex.Message}"); }
            return 0;
        }

        private int GetProcessedFilesForJob(BackupJob job)
        {
            if (job == null) return 0;
            int total = GetTotalFilesForJob(job);
            // Gérer le cas où total est 0 pour éviter la division par zéro.
            if (total == 0) return (job.Progress == 100 ? 0 : 0); // Si 0 fichiers, 0 traités (sauf si marqué 100% pour un job vide "complété")
            return (int)(total * (job.Progress / 100.0));
        }

        private void ClearPriorityFilesForJob(BackupJob job)
        {
            if (job == null) return;
            var keysToRemove = BackupController.RemainingPriorityFiles.Keys
                .Where(k => k.StartsWith(job.Name + "_"))
                .ToList();
            foreach (var key in keysToRemove)
            {
                BackupController.RemainingPriorityFiles.TryRemove(key, out _);
            }
        }

        #region Méthodes d'Orchestration Pause/Reprise (appelées par ViewModel et BusinessSoftwareMonitor)
        public void RequestUserPauseJob(string jobName)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Name == jobName);
            if (job != null && job.IsActive)
            {
                job.RequestUserPause();
                UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
                // _logger.LogSystemEvent($"Tâche '{job.Name}' mise en pause par l'utilisateur."); // Si vous implémentez LogSystemEvent
                System.Diagnostics.Debug.WriteLine($"ÉVÉNEMENT: Tâche '{job.Name}' mise en pause par l'utilisateur.");
            }
        }

        public void RequestUserResumeJob(string jobName)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Name == jobName);
            if (job != null && job.IsUserPaused && job.IsActive)
            {
                job.RequestUserResume();
                UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
                System.Diagnostics.Debug.WriteLine($"ÉVÉNEMENT: Tâche '{job.Name}' reprise par l'utilisateur.");
            }
        }

        public void PauseActiveJobsDueToSystem()
        {
            MainThread.BeginInvokeOnMainThread(async () => await Toast.Make("Logiciel métier détecté: Pause des sauvegardes...", ToastDuration.Short).Show());
            foreach (var job in _backupJobs.Where(j => j.IsActive))
            {
                job.RequestSystemPause();
                UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
                System.Diagnostics.Debug.WriteLine($"ÉVÉNEMENT: Tâche '{job.Name}' mise en pause (système).");
            }
        }

        public void ResumeSystemPausedJobs()
        {
            MainThread.BeginInvokeOnMainThread(async () => await Toast.Make("Logiciel métier arrêté: Reprise des sauvegardes...", ToastDuration.Short).Show());
            foreach (var job in _backupJobs.Where(j => j.IsSystemPaused && j.IsActive))
            {
                job.RequestSystemResume();
                UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
                System.Diagnostics.Debug.WriteLine($"ÉVÉNEMENT: Tâche '{job.Name}' reprise (système).");
            }
        }
        #endregion
        
        private void PerformBackupInternal(BackupJob job, bool IsCryptChecked, CancellationToken cancellationToken, IProgress<double> progressReporter)
        {
            int totalFiles = 0;
            int processedFilesCount = 0;
            PerformanceTimer fileProcessTimer = new PerformanceTimer();

            try
            {
                if (!Directory.Exists(job.SourcePath))
                {
                    _logger.LogBackupAction(job.Name, job.SourcePath, "N/A", 0, 0, 0, true);
                    UpdateRealTimeStateForJob(job, null, null, 0, 0, "ERROR_NO_SOURCE");
                    job.Progress = 100;
                    return;
                }
                if (!Directory.Exists(job.TargetPath)) Directory.CreateDirectory(job.TargetPath);

                var settings = AppSettings.Load();
                var encryptExtensions = settings.EncryptExtensions?.Select(e => e.ToLowerInvariant()).ToList() ?? new List<string>();

                string[] files = Directory.GetFiles(job.SourcePath, "*.*", SearchOption.AllDirectories);
                totalFiles = files.Length;
                if (totalFiles == 0)
                {
                    job.Progress = 100;
                    progressReporter?.Report(1.0);
                    UpdateRealTimeStateForJob(job, job.SourcePath, job.TargetPath, 0, 0, "COMPLETED_EMPTY");
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}': Source vide, rien à copier.", ToastDuration.Short).Show());
                    return;
                }
                job.Progress = 0;
                progressReporter?.Report(0);
                UpdateRealTimeStateForJob(job, job.SourcePath, job.TargetPath, totalFiles, 0, job.GetCurrentStatusDisplay());

                cancellationToken.ThrowIfCancellationRequested();
                job.PauseSignal.Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                UpdateRealTimeStateForJob(job, job.SourcePath, job.TargetPath, totalFiles, processedFilesCount, job.GetCurrentStatusDisplay());

                object lockForProcessedCount = new object();
                var priorityExtensions = settings.PriorityExtensions?.Select(e => e.ToLowerInvariant()).ToList() ?? new List<string>();
                long maxFileSizeBytes = (settings.FileMaxSizes?.FirstOrDefault() is string mfs && long.TryParse(mfs, out long mfk) ? mfk : 400) * 1024;

                ClearPriorityFilesForJob(job);
                foreach (string file in files.Where(f => priorityExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
                {
                    BackupController.RemainingPriorityFiles.TryAdd(job.Name + "_" + file, true);
                }

                Parallel.ForEach(files, new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
                (file, loopState) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    job.PauseSignal.Wait(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    string currentFileExtension = Path.GetExtension(file).ToLowerInvariant();
                    bool isCurrentFilePriority = priorityExtensions.Contains(currentFileExtension);
                    string currentFilePriorityKey = job.Name + "_" + file;

                    while (!isCurrentFilePriority && BackupController.RemainingPriorityFiles.Any(kvp => kvp.Key.StartsWith(job.Name + "_") && kvp.Value))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        job.PauseSignal.Wait(cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(50);
                    }

                    long currentFileSize = new FileInfo(file).Length;
                    bool isCurrentFileLarge = currentFileSize > maxFileSizeBytes;
                    bool largeFileSemHeld = false;
                    string destinationFilePath = Path.Combine(job.TargetPath, Path.GetRelativePath(job.SourcePath, file));
                    string destDir = Path.GetDirectoryName(destinationFilePath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    try
                    {
                        if (isCurrentFileLarge)
                        {
                            BackupController.LargeFileSemaphore.Wait(cancellationToken);
                            largeFileSemHeld = true;
                        }

                        lock (lockForProcessedCount) UpdateRealTimeStateForJob(job, file, destinationFilePath, totalFiles, processedFilesCount, job.GetCurrentStatusDisplay());

                        bool shouldEncryptFile = IsCryptChecked && encryptExtensions.Contains(currentFileExtension);
                        bool isSourceFileEncrypted = IsFileEncrypted(file);
                        double fileEncryptionTime = 0;
                        long fileCopyTime = 0;
                        bool wasFileSkipped = false;

                        bool areFilesIdentical = false;
                        if (job.Type == BackupType.Differential && File.Exists(destinationFilePath))
                        {
                            bool isDestFileEncrypted = IsFileEncrypted(destinationFilePath);
                            if (isSourceFileEncrypted == isDestFileEncrypted) areFilesIdentical = CompareFileHashes(file, destinationFilePath);
                        }

                        if (areFilesIdentical)
                        {
                            wasFileSkipped = true;
                        }
                        else
                        {
                            fileProcessTimer.Start();
                            _fileHelper.CopyFile(file, destinationFilePath);
                            fileProcessTimer.Stop();
                            fileCopyTime = (long)fileProcessTimer.GetElapsedMilliseconds();

                            if (!isSourceFileEncrypted && shouldEncryptFile)
                            {
                                lock (BackupController.CryptoSoftLock)
                                {
                                    var cryptoSoft = new EncryptWithCryptoSoft();
                                    var encTimer = Stopwatch.StartNew();
                                    bool encSuccess = cryptoSoft.EncryptFile(destinationFilePath, destinationFilePath);
                                    encTimer.Stop();
                                    fileEncryptionTime = encSuccess ? encTimer.Elapsed.TotalMilliseconds : -1;
                                }
                            }
                        }

                        lock (lockForProcessedCount)
                        {
                            processedFilesCount++;
                            job.Progress = (double)processedFilesCount / totalFiles * 100;
                            progressReporter?.Report(job.Progress / 100.0);
                            _logger.LogBackupAction(job.Name, file, destinationFilePath, currentFileSize, (double)fileCopyTime, fileEncryptionTime, wasFileSkipped); // jobStopped = wasFileSkipped
                            UpdateRealTimeStateForJob(job, file, destinationFilePath, totalFiles, processedFilesCount, job.GetCurrentStatusDisplay());
                        }

                        if (isCurrentFilePriority) BackupController.RemainingPriorityFiles.TryRemove(currentFilePriorityKey, out _);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur fichier '{Path.GetFileName(file)}' (Job '{job.Name}', TID {Thread.CurrentThread.ManagedThreadId}): {ex.Message}");
                        lock (lockForProcessedCount)
                        {
                            _logger.LogBackupAction(job.Name, file, destinationFilePath, currentFileSize, -1, -1, true);
                            UpdateRealTimeStateForJob(job, file, destinationFilePath, totalFiles, processedFilesCount, "ACTIVE_WITH_FILE_ERROR");
                        }
                    }
                    finally
                    {
                        if (largeFileSemHeld) BackupController.LargeFileSemaphore.Release();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"PerformBackupInternal pour '{job.Name}' annulé.");
                UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "CANCELLED");
                job.Progress = (double)processedFilesCount / (totalFiles > 0 ? totalFiles : 1) * 100;
                throw;
            }
            catch (AggregateException ae)
            {
                bool wasCancellation = false;
                ae.Handle(ex => {
                    if (ex is OperationCanceledException) { wasCancellation = true; return true; }
                    System.Diagnostics.Debug.WriteLine($"Erreur aggrégée dans PerformBackupInternal pour '{job.Name}': {ex.InnerException?.Message ?? ex.Message}");
                    return true;
                });
                job.Progress = (double)processedFilesCount / (totalFiles > 0 ? totalFiles : 1) * 100;
                UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, wasCancellation ? "CANCELLED" : "ERROR_AGGREGATE");
                if (wasCancellation) throw new OperationCanceledException("Sauvegarde annulée via AggregateException.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur globale dans PerformBackupInternal pour '{job.Name}': {ex.Message}");
                job.Progress = (double)processedFilesCount / (totalFiles > 0 ? totalFiles : 1) * 100;
                UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "ERROR_GLOBAL");
                throw;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                if (processedFilesCount == totalFiles)
                {
                    job.Progress = 100;
                    UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "COMPLETED");
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' terminé avec succès.", ToastDuration.Short).Show());
                }
                else
                {
                    UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "COMPLETED_WITH_ERRORS");
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' terminé (erreurs sur {totalFiles - processedFilesCount} fichier(s)).", ToastDuration.Long).Show());
                }
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

        public void RunBackupJobByIndex(int index, bool IsCryptChecked, IProgress<double> progressReporter = null)
        {
            throw new NotImplementedException();
        }
    }


    public static class BackupController
    {
        // Stocke l'état de chaque fichier prioritaire (true = en attente, false = terminé)
        public static ConcurrentDictionary<string, bool> RemainingPriorityFiles { get; } = new();

        // Contrôle le nombre de fichiers "lourds" (> maxFileSize) copiés en même temps
        public static SemaphoreSlim LargeFileSemaphore { get; } = new(1, 1);
        public static object CryptoSoftLock { get; } = new object();
    }

}
