using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using EasySave.Maui.Interfaces;
using EasySave.Maui.Localizations;
using EasySave.Maui.Logging;
using EasySave.Maui.Models;
using EasySave.Maui.Utils;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using EasySave.Maui.Localizations; 
using System.Security.Cryptography;
using System.Text;

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

        // Objet de verrouillage pour synchroniser les accès aux ressources partagées (logger, stateManager) depuis Parallel.ForEach
        private readonly object _logAndStateLock = new object();


        public BackupService(Logger logger , LocalizationService localizationService)
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
                System.Diagnostics.Debug.WriteLine($"SaveJobsToFile Error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"LoadJobsFromFile Error: {ex.Message}");
            }
        }

        public void CreateBackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {
            try
            {
                if (_backupJobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Un job '{name}' existe déjà.", ToastDuration.Short).Show());
                    return;
                }
                var newJob = new BackupJob(name, sourcePath, targetPath, type);
                _backupJobs.Add(newJob);
                SaveJobsToFile();
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{newJob.Name}' ajouté.", ToastDuration.Short).Show());
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Erreur création: {ex.Message}", ToastDuration.Short).Show());
            }
        }

        public void DeleteBackupJobByIndex(int index)
        {
            try
            {
                if (index < 0 || index >= _backupJobs.Count)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Index de job invalide pour suppression : {index}.", ToastDuration.Short).Show());
                    return;
                }
                var job = _backupJobs[index];
                if (job.IsActive)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' actif, ne peut être supprimé.", ToastDuration.Short).Show());
                    return;
                }
                _backupJobs.RemoveAt(index);
                SaveJobsToFile();
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' supprimé.", ToastDuration.Short).Show());
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Erreur suppression: {ex.Message}", ToastDuration.Short).Show());
            }
        }

        public void DeleteBackupJobByName(string name)
        {
            try
            {
                var job = _backupJobs.FirstOrDefault(j => j.Name == name);
                if (job != null)
                {
                    if (job.IsActive)
                    {
                        MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' actif, ne peut être supprimé.", ToastDuration.Short).Show());
                        return;
                    }
                    _backupJobs.Remove(job);
                    SaveJobsToFile();
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' supprimé.", ToastDuration.Short).Show());
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Erreur suppression: {ex.Message}", ToastDuration.Short).Show());
            }
        }

        private void UpdateRealTimeStateForJob(BackupJob job, string currentFileSource, string currentFileDest, int totalFiles, int filesDone, string explicitStatus = null)
        {
            if (job == null) return;
            var stateEntry = new BackupState(job.Name)
            {
                SourceFilePath = currentFileSource,
                TargetFilePath = currentFileDest,
                State = explicitStatus ?? job.GetCurrentStatusDisplay(),
                TotalFilesToCopy = totalFiles,
                NbFilesLeftToDo = totalFiles - filesDone,
                Progression = job.Progress
            };
            lock (_logAndStateLock)
            {
                _stateManager.UpdateState(stateEntry);
            }
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
            if (total == 0) return (job.Progress == 100 ? 0 : 0);
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


        #region Méthodes d'Orchestration Pause/Reprise
        public void RequestUserPauseJob(string jobName)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Name == jobName);
            if (job != null && job.IsActive)
            {
                job.RequestUserPause();
                UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
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

        public async Task RunBackupJobAsync(BackupJob job, bool isCryptChecked, CancellationToken cancellationToken, IProgress<double> progressReporter = null)
        {
            System.Diagnostics.Debug.WriteLine($"Début RunBackupJobAsync pour job: {job?.Name}");
            if (job == null) { System.Diagnostics.Debug.WriteLine("RunBackupJobAsync: job est null."); return; }

            if (job.IsActive)
            {
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make($"Job '{job.Name}' déjà en cours.", ToastDuration.Short).Show());
                return;
            }

            var settings = AppSettings.Load();
            System.Diagnostics.Debug.WriteLine($"Settings chargés: {settings != null}");
            if (IsBusinessSoftwareRunning(settings.Softwares ?? new List<string>()))
            {
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make("Logiciel métier actif. Lancement annulé.", ToastDuration.Long).Show());
                // Utiliser un lock pour le logger si appelé depuis un contexte potentiellement concurrent
                lock (_logAndStateLock) _logger.LogBackupAction(job.Name, "N/A", "N/A", 0, 0, 0, true);
                return;
            }

            System.Diagnostics.Debug.WriteLine("Initialisation des propriétés du job");
            try
            {
                job.ResetPauseStatesForNewRun(); // Utilise la méthode de BackupJob pour réinitialiser
                job.IsActive = true;
                job.LastRun = DateTime.Now;
                job.Progress = 0;
                progressReporter?.Report(0);

                UpdateRealTimeStateForJob(job, null, null, 0, 0, job.GetCurrentStatusDisplay());
                SaveJobsToFile();

                System.Diagnostics.Debug.WriteLine("Lancement de PerformBackupInternal");
                await Task.Run(() => PerformBackupInternal(job, isCryptChecked, cancellationToken, progressReporter), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Job '{job.Name}' annulé via CancellationToken.");
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make($"Job '{job.Name}' annulé.", ToastDuration.Short).Show());
                lock (_logAndStateLock) _logger.LogBackupAction(job.Name, "N/A", "N/A", 0, 0, 0, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans RunBackupJobAsync pour '{job.Name}': {ex.Message}\n{ex.StackTrace}");
                await MainThread.InvokeOnMainThreadAsync(() => Toast.Make($"Erreur Job '{job.Name}': {ex.Message}", ToastDuration.Long).Show());
                lock (_logAndStateLock) _logger.LogBackupAction(job.Name, "N/A", "N/A", 0, 0, 0, true);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"Fin RunBackupJobAsync (finally) pour job: {job?.Name}");
                try
                {
                    job.NotifyCompletionOrStop();
                    progressReporter?.Report(job.Progress / 100.0);
                    UpdateRealTimeStateForJob(job, null, null, GetTotalFilesForJob(job), GetProcessedFilesForJob(job), job.GetCurrentStatusDisplay());
                    SaveJobsToFile();
                    ClearPriorityFilesForJob(job);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur dans le bloc finally de RunBackupJobAsync: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public void UpdateState(BackupJob newJob) 
        {
            var existingState = _backupJobs.Find(s => s.Name == newJob.Name); // pas utilisé
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
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Erreur sauvegarde état jobs: {ex.Message}", ToastDuration.Short).Show());
            }
        }


        public void ListBackupJobs()
        {
            if (!_backupJobs.Any())
            {
                System.Diagnostics.Debug.WriteLine("Aucun travail de sauvegarde défini.");
                return;
            }
            System.Diagnostics.Debug.WriteLine("--- Liste des Travaux de Sauvegarde ---");
            for (int i = 0; i < _backupJobs.Count; i++)
            {
                var job = _backupJobs[i];
                System.Diagnostics.Debug.WriteLine($"{i + 1}. {job.Name} ({job.Type}) - Actif: {job.IsActive}");
            }
        }

        public IReadOnlyList<BackupJob> GetJobs()
        {
            return _backupJobs.AsReadOnly();
        }

        private void PerformBackupInternal(BackupJob job, bool IsCryptChecked, CancellationToken cancellationToken, IProgress<double> progressReporter)
        {
            int totalFiles = 0;
            int processedFilesCount = 0;
            PerformanceTimer fileProcessTimer = new PerformanceTimer(); 

            try
            {
                if (!Directory.Exists(job.SourcePath))
                {
                    lock (_logAndStateLock) _logger.LogBackupAction(job.Name, job.SourcePath, "N/A", 0, 0, 0, true);
                    UpdateRealTimeStateForJob(job, null, null, 0, 0, "ERROR_NO_SOURCE");
                    job.Progress = 100; // Marquer comme "terminé" pour ne pas bloquer indéfiniment
                    progressReporter?.Report(1.0);
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
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}': Source vide.", ToastDuration.Short).Show());
                    return;
                }
                job.Progress = 0;
                progressReporter?.Report(0);
                UpdateRealTimeStateForJob(job, job.SourcePath, job.TargetPath, totalFiles, 0, job.GetCurrentStatusDisplay());

                cancellationToken.ThrowIfCancellationRequested();
                job.PauseSignal.Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                UpdateRealTimeStateForJob(job, job.SourcePath, job.TargetPath, totalFiles, processedFilesCount, job.GetCurrentStatusDisplay());

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

                    long currentFileSize = 0;
                    try { currentFileSize = new FileInfo(file).Length; } catch { /* ignorer si le fichier disparaît */ }

                    bool isCurrentFileLarge = currentFileSize > maxFileSizeBytes;
                    bool largeFileSemHeld = false;
                    string destinationFilePath = Path.Combine(job.TargetPath, Path.GetRelativePath(job.SourcePath, file));
                    string destDir = Path.GetDirectoryName(destinationFilePath);

                    try
                    {
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                        if (isCurrentFileLarge)
                        {
                            BackupController.LargeFileSemaphore.Wait(cancellationToken);
                            largeFileSemHeld = true;
                        }

                        lock (_logAndStateLock) UpdateRealTimeStateForJob(job, file, destinationFilePath, totalFiles, processedFilesCount, job.GetCurrentStatusDisplay());

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

                        lock (_logAndStateLock) // Synchroniser l'accès au logger et au state manager
                        {
                            processedFilesCount++;
                            job.Progress = (double)processedFilesCount / totalFiles * 100;
                            progressReporter?.Report(job.Progress / 100.0);
                            _logger.LogBackupAction(job.Name, file, destinationFilePath, currentFileSize, (double)fileCopyTime, fileEncryptionTime, wasFileSkipped);
                            UpdateRealTimeStateForJob(job, file, destinationFilePath, totalFiles, processedFilesCount, job.GetCurrentStatusDisplay());
                        }

                        if (isCurrentFilePriority) BackupController.RemainingPriorityFiles.TryRemove(currentFilePriorityKey, out _);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur fichier '{Path.GetFileName(file)}' (Job '{job.Name}', TID {Thread.CurrentThread.ManagedThreadId}): {ex.Message}");
                        lock (_logAndStateLock)
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
                job.Progress = totalFiles > 0 ? (double)processedFilesCount / totalFiles * 100 : 0;
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
                job.Progress = totalFiles > 0 ? (double)processedFilesCount / totalFiles * 100 : 0;
                UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, wasCancellation ? "CANCELLED" : "ERROR_AGGREGATE");
                if (wasCancellation) throw new OperationCanceledException("Sauvegarde annulée via AggregateException.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur globale dans PerformBackupInternal pour '{job.Name}': {ex.Message}");
                job.Progress = totalFiles > 0 ? (double)processedFilesCount / totalFiles * 100 : 0;
                UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "ERROR_GLOBAL");
                throw;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                if (processedFilesCount == totalFiles && totalFiles > 0) 
                {
                    job.Progress = 100;
                    UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "COMPLETED");
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' terminé avec succès.", ToastDuration.Short).Show());
                }
                else if (totalFiles > 0) 
                {
                    UpdateRealTimeStateForJob(job, null, null, totalFiles, processedFilesCount, "COMPLETED_WITH_ERRORS");
                    MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Job '{job.Name}' terminé (erreurs sur {totalFiles - processedFilesCount} fichier(s)).", ToastDuration.Long).Show());
                }
            }
        }

        private static bool CompareFileHashes(string file1Path, string file2Path)
        {
            try
            {
                using var sha256 = SHA256.Create();
                byte[] hash1, hash2;
                using (var stream1 = File.OpenRead(file1Path)) hash1 = sha256.ComputeHash(stream1);
                using (var stream2 = File.OpenRead(file2Path)) hash2 = sha256.ComputeHash(stream2);
                return hash1.SequenceEqual(hash2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur CompareFileHashes: {ex.Message}");
                return false;
            }
        }

        private bool IsBusinessSoftwareRunning(List<string> softwareNames)
        {
            try
            {
                foreach (var softwareName in softwareNames.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var processNameOnly = Path.GetFileNameWithoutExtension(softwareName);
                    if (Process.GetProcessesByName(processNameOnly).Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Logiciel métier détecté : {processNameOnly}");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur vérification logiciels métiers : {ex.Message}");
                return false;
            }
        }

        private bool IsFileEncrypted(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 8) return false;
                    byte[] buffer = new byte[8];
                    fs.Read(buffer, 0, buffer.Length);
                    string headerAsString = Encoding.UTF8.GetString(buffer);
                    string decryptedHeader = XOREncrypt(headerAsString, "Ces1Kryp");
                    return decryptedHeader.StartsWith("Ces1Kryp");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur IsFileEncrypted pour {filePath}: {ex.Message}");
                return false;
            }
        }

        private string XOREncrypt(string data, string key = "Ces1Kryp") 
        {
            var dataLen = data.Length;
            var keyLen = key.Length;
            if (keyLen == 0) return data; 
            char[] output = new char[dataLen];
            for (var i = 0; i < dataLen; ++i)
            {
                output[i] = (char)(data[i] ^ key[i % keyLen]);
            }
            return new string(output);
        }

        
        public void RunBackupJobByIndex(int index, bool IsCryptChecked)
        {
            System.Diagnostics.Debug.WriteLine($"RunBackupJobByIndex (sync) appelée. Redirection vers Async. Pensez à mettre à jour IBackupService.");
            if (index < 0 || index >= _backupJobs.Count)
            {
                MainThread.BeginInvokeOnMainThread(async () => await Toast.Make($"Le job avec n° {index + 1} est inexistant", ToastDuration.Short).Show());
                return;
            }
            var job = _backupJobs[index];
            _ = RunBackupJobAsync(job, IsCryptChecked, CancellationToken.None, null);
        }
        public void RunBackupJob(BackupJob job, bool isCryptChecked)
        {
            System.Diagnostics.Debug.WriteLine($"RunBackupJob (sync) appelée pour '{job?.Name}'. Redirection vers Async.");
            _ = RunBackupJobAsync(job, isCryptChecked, CancellationToken.None, null);
        }

    }

    public static class BackupController
    {
        public static ConcurrentDictionary<string, bool> RemainingPriorityFiles { get; } = new();
        public static SemaphoreSlim LargeFileSemaphore { get; } = new(1, 1);
        public static object CryptoSoftLock { get; } = new object();
    }
}
