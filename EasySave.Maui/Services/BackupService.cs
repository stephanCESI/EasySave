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
            catch (Exception ex )
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
                Toast.Make($"Le job {job.Name} a été réalisé avec succès.", ToastDuration.Short).Show();
            }
            catch(Exception ex)
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
                Toast.Make($"Le job {job.Name} a été réalisé avec succès.", ToastDuration.Short).Show();
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
                    return;

                if (!Directory.Exists(job.TargetPath))
                    Directory.CreateDirectory(job.TargetPath);

                var settings = AppSettings.Load();
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

                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(job.SourcePath, file);
                    string destinationFile = Path.Combine(job.TargetPath, relativePath);
                    long fileSize = new FileInfo(file).Length;
                    bool shouldEncrypt = IsCryptChecked && encryptExtensions.Contains(Path.GetExtension(file).ToLower());
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
                                System.Console.WriteLine($"Fichier inchangé : {file}");
                                processedFiles++;
                                continue;
                            }
                        }

                        _timer.Start();
                        _fileHelper.CopyFile(file, destinationFile);
                        _timer.Stop();
                        System.Console.WriteLine($"Fichier copié : {file} -> {destinationFile}");

                        if (sourceEncrypted)
                        {
                            System.Console.WriteLine($"Fichier déjà chiffré : {file}, aucune action de chiffrement.");
                        }
                        else if (shouldEncrypt)
                        {
                            try
                            {
                                var encryptionTimer = System.Diagnostics.Stopwatch.StartNew();
                                bool success = cryptoService.EncryptFile(destinationFile, destinationFile);
                                encryptionTimer.Stop();

                                if (success)
                                {
                                    encryptionTime = encryptionTimer.Elapsed.TotalMilliseconds;
                                    Toast.Make($"Fichier chiffré : {file} -> {destinationFile} (en {encryptionTime} ms)", ToastDuration.Short).Show();
                                    
                                }
                                else
                                {
                                    encryptionTime = -1;
                                    Toast.Make($"Échec du chiffrement du fichier : {file}", ToastDuration.Short).Show();
                                }
                            }
                            catch (Exception ex)
                            {
                                encryptionTime = -1;
                                Toast.Make($"Erreur lors du chiffrement du fichier {file} : {ex.Message}", ToastDuration.Short).Show();
                                
                            }
                        }

                        processedFiles++;
                    }
                    catch (Exception ex)
                    {
                        Toast.Make(ex.Message, ToastDuration.Short).Show();
                    }

                    double progression = (double)processedFiles / totalFiles * 100;
                    state.NbFilesLeftToDo = totalFiles - processedFiles;
                    state.Progression = progression;

                    _stateManager.UpdateState(state);
                    _logger.LogBackupAction(job.Name, job.SourcePath, job.TargetPath, fileSize, _timer.GetElapsedMilliseconds(), encryptionTime, false);
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
                System.Console.WriteLine($"Erreur lors de la vérification du chiffrement du fichier {filePath} : {ex.Message}");
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
}
