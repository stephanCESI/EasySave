using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    public interface IBackupService
    {
        void CreateBackupJob(string name, string sourcePath, string targetPath, BackupType type);
        void DeleteBackupJobByIndex(int index); // Modification pour accepter un index
        void RunBackupJobByIndex(int index);    // Modification pour accepter un index
        void ListBackupJobs();
    }
}
