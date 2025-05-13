using System;

namespace EasySave.Core.Models
{
    public class BackupState
    {
        public string Name { get; set; }
        public string SourceFilePath { get; set; }
        public string TargetFilePath { get; set; }
        public string State { get; set; }
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        public int NbFilesLeftToDo { get; set; }
        public double Progression { get; set; }
        public DateTime LastUpdate { get; set; }

        public BackupState(string name)
        {
            Name = name;
            SourceFilePath = "";
            TargetFilePath = "";
            State = "END";
            TotalFilesToCopy = 0;
            TotalFilesSize = 0;
            NbFilesLeftToDo = 0;
            Progression = 0;
            LastUpdate = DateTime.Now;
        }
    }
}
