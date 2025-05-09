using System;
using System.IO;
using System.Xml;
using EasySave.Core.Models;
using Newtonsoft.Json;

namespace EasySave.Logging
{
    public class JsonLogWriter : ILogWriter
    {
        private readonly string _logDirectory;

        public JsonLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void WriteLog(LogEntry logEntry)
        {
            string filePath = Path.Combine(_logDirectory, $"{logEntry.Time:yyyy-MM-dd}.json");

            // Si le fichier existe déjà, nous ajoutons la nouvelle entrée.
            var logEntries = File.Exists(filePath)
                ? JsonConvert.DeserializeObject<List<LogEntry>>(File.ReadAllText(filePath)) ?? new List<LogEntry>()
                : new List<LogEntry>();

            logEntries.Add(logEntry);

            // On réécrit tout le fichier JSON avec les nouvelles données.
            File.WriteAllText(filePath, JsonConvert.SerializeObject(logEntries, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
