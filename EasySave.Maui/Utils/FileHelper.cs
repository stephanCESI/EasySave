using System;
using System.IO;
using EasySave.Maui.Models;

namespace EasySave.Maui.Utils
{
    public class FileHelper
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void CopyFile(string source, string target, bool overwrite = true)
        {
            try
            {
                string? directory = Path.GetDirectoryName(target);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(source, target, overwrite);
                Console.WriteLine($"Fichier copié : {source} -> {target}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la copie du fichier '{source}': {ex.Message}");
            }
        }

        public void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Console.WriteLine($"Fichier supprimé : {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression du fichier '{path}': {ex.Message}");
            }
        }

        public long GetFileSize(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération de la taille du fichier : {ex.Message}");
                return -1;
            }
        }

        public DateTime GetLastModifiedTime(string path)
        {
            try
            {
                return File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération de la date de modification : {ex.Message}");
                return DateTime.MinValue;
            }
        }
    }
}
