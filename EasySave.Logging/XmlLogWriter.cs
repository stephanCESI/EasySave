using System;
using System.IO;
using System.Xml;
using EasySave.Core.Models;

namespace EasySave.Logging
{
    public class XmlLogWriter : ILogWriter
    {
        private readonly string _logDirectory;

        public XmlLogWriter(string logDirectory)
        {
            _logDirectory = logDirectory;
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void WriteLog(LogEntry logEntry)
        {
            string filePath = Path.Combine(_logDirectory, $"{logEntry.Time:yyyy-MM-dd}.xml");

            // Initialiser le document XML
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root;

            // Charger ou créer le fichier XML
            if (File.Exists(filePath))
            {
                xmlDoc.Load(filePath);
                root = xmlDoc.DocumentElement;
            }
            else
            {
                root = xmlDoc.CreateElement("Logs");
                xmlDoc.AppendChild(root);
            }

            // Créer un nouvel élément de log
            XmlElement logElement = xmlDoc.CreateElement("LogEntry");

            void AddElement(string name, string value)
            {
                XmlElement element = xmlDoc.CreateElement(name);
                element.InnerText = value;
                logElement.AppendChild(element);
            }

            AddElement("Name", logEntry.Name);
            AddElement("FileSource", logEntry.FileSource);
            AddElement("FileTarget", logEntry.FileTarget);
            AddElement("FileSize", logEntry.FileSize.ToString());
            AddElement("FileTransferTime", logEntry.FileTransferTime.ToString());
            AddElement("Time", logEntry.Time.ToString("O"));

            // Ajouter l'élément au document
            root.AppendChild(logElement);

            // Sauvegarder le fichier XML
            xmlDoc.Save(filePath);
        }
    }
}
