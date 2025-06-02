using System;
using System.Text.Json.Serialization;

namespace EasySave.Maui.Models
{
    public class BackupJob
    {
        private ManualResetEventSlim _pauseSignal = new ManualResetEventSlim(true);
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public BackupType Type { get; set; }
        public DateTime LastRun { get; set; }
        public bool IsActive { get; set; }
        public double Progress { get; set; }

        [JsonIgnore]
        public bool IsUserPaused { get; set; } = false;

        [JsonIgnore]
        public bool IsSystemPaused { get; set; } = false; // Pause due au logiciel métier

        [JsonIgnore]
        public ManualResetEventSlim PauseSignal => _pauseSignal;

        public BackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {
            Name = name;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            Type = type;
            LastRun = DateTime.MinValue;
            IsActive = false;
        }

        public BackupJob()
        {
            _pauseSignal = new ManualResetEventSlim(true);
            Progress = 0;
        }

        public string GetCurrentStatusDisplay() 
        {
            if (!IsActive && LastRun == DateTime.MinValue) return "Jamais exécutée";
            if (!IsActive) return "Inactive";
            if (IsSystemPaused && IsUserPaused) return "En pause (Système & Utilisateur)";
            if (IsSystemPaused) return "En pause (Système)";
            if (IsUserPaused) return "En pause (Utilisateur)";
            return "En cours";
        }

        public void RequestUserPause()
        {
            if (!IsActive) return;
            IsUserPaused = true;
            PauseSignal.Reset(); 
        }

        public void RequestUserResume()
        {
            IsUserPaused = false;
            if (!IsSystemPaused) 
            {
                PauseSignal.Set(); 
            }
        }

        public void RequestSystemPause()
        {
            if (!IsActive) return;
            IsSystemPaused = true;
            PauseSignal.Reset();
        }

        public void RequestSystemResume()
        {
            IsSystemPaused = false;
            if (!IsUserPaused)
            {
                PauseSignal.Set();
            }
        }

        public void NotifyCompletionOrStop()
        {
            System.Diagnostics.Debug.WriteLine($"NotifyCompletionOrStop pour {Name}");
            IsActive = false;
            IsUserPaused = false;
            IsSystemPaused = false;
            try
            {
                PauseSignal.Set();
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debug.WriteLine($"PauseSignal déjà disposé pour {Name}");
            }
        }

        public void ResetPauseStatesForNewRun()
        {
            System.Diagnostics.Debug.WriteLine($"ResetPauseStatesForNewRun pour {Name}");
            IsUserPaused = false;
            IsSystemPaused = false;
            try
            {
                _pauseSignal.Dispose(); 
                _pauseSignal = new ManualResetEventSlim(true); 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la réinitialisation de PauseSignal pour {Name}: {ex.Message}");
                _pauseSignal = new ManualResetEventSlim(true); 
            }
        }
    }
}
