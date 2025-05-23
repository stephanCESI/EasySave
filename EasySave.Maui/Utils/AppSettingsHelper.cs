using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace EasySave.Maui.Utils;
public static class AppSettingsHelper
{

    private static readonly string basePath = AppContext.BaseDirectory;
    private static readonly string projectRoot = Path.Combine(basePath, "..", "..", "..", "..", "..");
    private static readonly string utilsPath = Path.Combine(projectRoot, "Utils", "AppSettings.json");
    private static readonly string SettingsFilePath = Path.GetFullPath(utilsPath);

    public static void SetLogFileType(string logFileType)
    {
        if (!File.Exists(SettingsFilePath))
            return;

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (settings != null)
            {
                settings["LogFileType"] = logFileType;
                string updatedJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, updatedJson);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la mise à jour des paramètres : {ex.Message}");
        }
    }

    public static string GetLogFileType()
    {
        var settings = AppSettings.Load();
        return settings.LogFileType;
    }

    private static void Save(AppSettings settings)
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Console.WriteLine($"Le fichier de configuration '{SettingsFilePath}' est introuvable.");
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var configData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (configData != null)
            {
                configData["EncryptExtensions"] = settings.EncryptExtensions;
                configData["Softwares"] = settings.Softwares;
                configData["PriorityExtensions"] = settings.PriorityExtensions;
                configData["FileMaxSizes"] = settings.FileMaxSizes;

                string updatedJson = JsonConvert.SerializeObject(configData, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, updatedJson);
            }
            else
            {
                Console.WriteLine("Erreur lors de la désérialisation du fichier de configuration.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde des paramètres : {ex.Message}");
        }
    }



    public static void SetEncryptExtensions(List<string> extensions)
    {
        var settings = AppSettings.Load();
        settings.EncryptExtensions = extensions;
        Save(settings);
    }

    public static List<string> GetEncryptExtensions()
    {
        var settings = AppSettings.Load();
        return settings.EncryptExtensions ?? new List<string>();
    }

    public static void SetSoftwares(List<string> softwares)
    {
        var settings = AppSettings.Load();
        settings.Softwares = softwares;
        Save(settings);
    }

    public static List<string> GetSoftwares()
    {
        var settings = AppSettings.Load();
        return settings.Softwares ?? new List<string>();
    }

    public static void SetPriorityExtensions(List<string> priorityExtensions)
    {
        var settings = AppSettings.Load();
        settings.PriorityExtensions = priorityExtensions;
        Save(settings);
    }

    public static List<string> GetPriorityExtensions()
    {
        var settings = AppSettings.Load();
        return settings.PriorityExtensions ?? new List<string>();
    }

    public static void SetMaxFileSizes(List<string> maxFileSizes)
    {
        var settings = AppSettings.Load();
        settings.FileMaxSizes = maxFileSizes;
        Save(settings);
    }

    public static List<string> GetMaxFileSizes()
    {
        var settings = AppSettings.Load();
        return settings.FileMaxSizes ?? new List<string>();
    }

}
