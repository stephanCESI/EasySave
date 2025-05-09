using System;
using System.IO;
using EasySave.Core.Interfaces;

namespace EasySave.Core.Services
{
    public class DirectoryManager
    {
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Console.WriteLine($"Répertoire créé : {path}");
                }
                else
                {
                    Console.WriteLine($"Le répertoire existe déjà : {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création du répertoire : {ex.Message}");
            }
        }

        public string[] GetFiles(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des fichiers : {ex.Message}");
                return Array.Empty<string>();
            }
        }

        public string[] GetSubDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des sous-répertoires : {ex.Message}");
                return Array.Empty<string>();
            }
        }
    }
}
