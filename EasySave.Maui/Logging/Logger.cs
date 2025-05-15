using System;
using EasySave.Maui.Interfaces;
using EasySave.Maui.Models;

namespace EasySave.Maui.Logging
{
    public class Logger
    {
        private ILogWriter _logWriter;

        public Logger(string logDirectory, string logFileType)
        {
            // Choisir le writer en fonction du type de fichier de log
            _logWriter = logFileType.ToLower() switch
            {
                "xml" => new XmlLogWriter(logDirectory),
                _ => new JsonLogWriter(logDirectory) // Par défaut : JSON
            };
        }

        public void LogBackupAction(string name, string fileSource, string fileTarget, long fileSize, double fileTransferTime, double encryptionTime, bool jobStopped)
        {
            var logEntry = new LogEntry(name, fileSource, fileTarget, fileSize, fileTransferTime, DateTime.Now, encryptionTime, jobStopped);
            _logWriter.WriteLog(logEntry);
            System.Console.WriteLine($"Log créé pour le fichier : {fileSource} -> {fileTarget}");
        }
    }
}
