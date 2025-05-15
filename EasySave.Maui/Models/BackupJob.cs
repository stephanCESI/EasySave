using System;

namespace EasySave.Maui.Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public BackupType Type { get; set; }
        public DateTime LastRun { get; set; }
        public bool IsActive { get; set; } 

        public BackupJob(string name, string sourcePath, string targetPath, BackupType type)
        {
            Name = name;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            Type = type;
            LastRun = DateTime.MinValue;
            IsActive = false;
        }
    }
}
