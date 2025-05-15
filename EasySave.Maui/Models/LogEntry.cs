using System;

namespace EasySave.Maui.Models
{
    public class LogEntry
    {
        public string Name { get; set; }
        public string FileSource { get; set; }
        public string FileTarget { get; set; }
        public long FileSize { get; set; }
        public double FileTransferTime { get; set; }
        public DateTime Time { get; set; }
        public double EncryptionTime { get; set; }
        public bool JobStopped {  get; set; }

        public LogEntry(string name, string fileSource, string fileTarget, long fileSize, double fileTransferTime, DateTime time, double encryptionTime, bool jobStopped)
        {
            Name = name;
            FileSource = fileSource;
            FileTarget = fileTarget;
            FileSize = fileSize;
            FileTransferTime = fileTransferTime;
            Time = time;
            EncryptionTime = encryptionTime;
            JobStopped = jobStopped;
        }
    }
}
