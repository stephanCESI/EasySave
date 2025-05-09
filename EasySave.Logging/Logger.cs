using System;
using EasySave.Core.Models;

namespace EasySave.Logging
{
    public class Logger
    {
        private readonly ILogWriter _logWriter;

        public Logger(ILogWriter logWriter)
        {
            _logWriter = logWriter;
        }

        public void LogBackupAction(string name, string fileSource, string fileTarget, long fileSize, double fileTransferTime)
        {
            var logEntry = new LogEntry(name, fileSource, fileTarget, fileSize, fileTransferTime, DateTime.Now);
            _logWriter.WriteLog(logEntry);
            System.Console.WriteLine($"Log créé pour le fichier : {fileSource} -> {fileTarget}");
        }
    }
}
