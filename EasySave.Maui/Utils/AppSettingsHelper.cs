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

}
