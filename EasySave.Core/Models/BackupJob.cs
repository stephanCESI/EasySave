using System;

namespace EasySave.Core.Models
{
    public class BackupJob
    {
        public string Name { get; set; }                 // Nom du travail de sauvegarde
        public string SourcePath { get; set; }            // Répertoire source
        public string TargetPath { get; set; }            // Répertoire cible
        public BackupType Type { get; set; }              // Type de sauvegarde (complète ou différentielle)
        public DateTime LastRun { get; set; }             // Dernière exécution
        public bool IsActive { get; set; }                // Statut actif ou non

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
