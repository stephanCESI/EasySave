using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Maui.Localizations;
using EasySave.Maui.Models;
using EasySave.Maui.Services;
using EasySave.Maui.Utils;
using System.Collections.ObjectModel;

namespace EasySave.Maui.ViewModels;
public partial class MainViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly LocalizationService _localizationService;

    public ObservableCollection<BackupJob> Jobs { get; } = new();

    [ObservableProperty]
    private string jobName;

    [ObservableProperty]
    private string sourcePath;

    [ObservableProperty]
    private string targetPath;

    [ObservableProperty]
    private BackupType selectedBackupType;

    [ObservableProperty]
    bool isVisibleAddJob;

    [ObservableProperty]
    bool isVisibleDeleteJob;

    [ObservableProperty]
    bool isVisibleCreateSelection;

    [ObservableProperty]
    string selectedJobsText;

    [ObservableProperty]
    private bool isFrench;

    [ObservableProperty]
    private string addButtonText;

    [ObservableProperty]
    private ObservableCollection<BackupJob> selectedJobs = new();

    partial void OnSelectedJobsChanged(ObservableCollection<BackupJob> value)
    {
        OnPropertyChanged(nameof(IsDeleteButtonEnabled));
    }

    public bool IsDeleteButtonEnabled => SelectedJobs.Count > 0;

    public Array BackupTypes { get; } = Enum.GetValues(typeof(BackupType));

    private bool _isXmlLog;
    public bool IsXmlLog
    {
        get => _isXmlLog;
        set
        {
            if (SetProperty(ref _isXmlLog, value))
            {
                // Met à jour le type de log dans le fichier de configuration
                var logFileType = _isXmlLog ? "xml" : "json";
                AppSettingsHelper.SetLogFileType(logFileType);
                Console.WriteLine($"LogFileType mis à jour : {logFileType}");
            }
        }
    }

    

    private void UpdateTexts()
    {
        AddButtonText = _localizationService.GetLocalizedString("addJob");
    }

    partial void OnIsFrenchChanged(bool value)
    {
        string language = value ? "fr" : "en";
        _localizationService.SetLanguage(language);
        UpdateTexts();
        OnPropertyChanged(nameof(IsFrench));
    }

    [ObservableProperty]
    public string selectedJobNames;

    public MainViewModel(BackupService backupService, LocalizationService localizationService)
    {
        _backupService = backupService;
        _localizationService = localizationService;
        LoadJobs();
        IsVisibleAddJob = false;
        IsVisibleDeleteJob = false;
        IsVisibleCreateSelection = false;
        

        IsFrench = LanguageHelper.GetCurrentLanguage(_localizationService) == "fr";
        UpdateTexts();

        var logFileType = AppSettingsHelper.GetLogFileType();
        IsXmlLog = logFileType.Equals("xml", StringComparison.OrdinalIgnoreCase);
    }

    public void LoadJobs()
    {
        Jobs.Clear();
        foreach (var job in _backupService.GetJobs())
            Jobs.Add(job);
    }

    [RelayCommand]
    private void OpenAddJobPopUp()
    {
        IsVisibleAddJob = true;
    }

    [RelayCommand]
    private void OpenDeleteJobPopUp()
    {
        IsVisibleDeleteJob = true;

        SelectedJobNames = string.Join(", ", SelectedJobs.Select(job => job.Name));
    }

    [RelayCommand]
    private void OpenPopUpCreateSelection()
    {
        IsVisibleCreateSelection = true;


    }

    [RelayCommand]
    private void ClickCancelButton()
    {
        IsVisibleAddJob = false;
        IsVisibleDeleteJob = false;
        IsVisibleCreateSelection = false;


    }

    [RelayCommand]
    private void AddJob()
    {
        if (string.IsNullOrWhiteSpace(JobName) || string.IsNullOrWhiteSpace(SourcePath)
           || string.IsNullOrWhiteSpace(TargetPath))
        {
            return;
        }

        // Nettoyer les chemins en enlevant les guillemets superflus
        var cleanedSourcePath = SourcePath.Trim('"');
        var cleanedTargetPath = TargetPath.Trim('"');

        _backupService.CreateBackupJob(JobName, cleanedSourcePath, cleanedTargetPath, selectedBackupType);
        LoadJobs();
        IsVisibleAddJob = false;

        JobName = string.Empty;
        SourcePath = string.Empty;
        TargetPath = string.Empty;
        SelectedBackupType = BackupType.Full;
    }


    [RelayCommand]
    private void DeleteSelectedJobs()
    {
        if (SelectedJobs == null || SelectedJobs.Count == 0)
        {

            IsVisibleDeleteJob = false;
            return;
        }

        foreach (var job in SelectedJobs.ToList()) // Créer une copie pour éviter les erreurs de modification
        {
            _backupService.DeleteBackupJobByName(job.Name);
            Jobs.Remove(job);
        }
        IsVisibleDeleteJob = false;
        SelectedJobs.Clear();
    }

    [RelayCommand]
    private void RunSelectedJobs()
    {
        if (SelectedJobs == null || SelectedJobs.Count == 0)
            return;

        foreach (var job in SelectedJobs.ToList()) // copie pour éviter modification pendant itération
        {
            _backupService.RunBackupJob(job);
        }
    }

    [RelayCommand]
    private void RunAllJobs()
    {
        foreach (var job in Jobs)
        {
            _backupService.RunBackupJob(job);
        }
    }

    [RelayCommand]
    private void ToggleLogFileType(bool isXml)
    {
        var logFileType = isXml ? "xml" : "json";
        AppSettingsHelper.SetLogFileType(logFileType);

        // Si besoin, tu peux aussi recharger le logger ici avec la nouvelle config
    }

    [RelayCommand]
    private void CreateSelectionJobs()
    {
        var selectedJobs = new List<int>();

        if (string.IsNullOrWhiteSpace(SelectedJobsText))
            return;

        var cleanedText = SelectedJobsText.Replace(" ", "");

        var parts = cleanedText.Split('-');

        foreach (var part in parts)
        {
            if (part.Contains(";"))
            {
                var range = part.Split(';');
                if (range.Length == 2 &&
                    int.TryParse(range[0], out int start) &&
                    int.TryParse(range[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                        selectedJobs.Add(i);
                }
            }
            else if (int.TryParse(part, out int number))
            {
                
                selectedJobs.Add(number);
            }
            
        }

        foreach (var job in selectedJobs)
        {
            _backupService.RunBackupJobByIndex(job);
        }

        IsVisibleCreateSelection = false;
        SelectedJobsText = "" ;
    }

}
