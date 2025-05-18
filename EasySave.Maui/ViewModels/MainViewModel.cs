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
    bool isVisibleParameters;

    [ObservableProperty]
    private bool isFrench;

    [ObservableProperty]
    private string addButtonText;

    [ObservableProperty]
    private string newExtension;

    [ObservableProperty]
    private string newSoftware;

    [ObservableProperty]
    private bool isCryptChecked;

    [ObservableProperty]
    private ObservableCollection<string> extensions = new();

    [ObservableProperty]
    private ObservableCollection<string> softwares = new();

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
                var logFileType = _isXmlLog ? "xml" : "json";
                AppSettingsHelper.SetLogFileType(logFileType);
                OnPropertyChanged(nameof(CurrentLogFormat));
                Console.WriteLine($"LogFileType mis à jour : {logFileType}");
            }
        }
    }

    public string CurrentLogFormat => IsXmlLog ? "xml" : "json";

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
        LoadExtensionsAndSoftwares();
        IsVisibleAddJob = false;
        IsVisibleDeleteJob = false;
        IsVisibleCreateSelection = false;
        IsVisibleParameters = false;

        IsFrench = LanguageHelper.GetCurrentLanguage(_localizationService) == "fr";
        UpdateTexts();

        var logFileType = AppSettingsHelper.GetLogFileType();
        IsXmlLog = logFileType.Equals("xml", StringComparison.OrdinalIgnoreCase);

        var settings = AppSettings.Load();
        Extensions = new ObservableCollection<string>(settings.EncryptExtensions);
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
        if (SelectedJobs == null || SelectedJobs.Count == 0)
        {
            return;
        }

        IsVisibleDeleteJob = true;

        SelectedJobNames = string.Join(", ", SelectedJobs.Select(job => job.Name));
    }

    [RelayCommand]
    private void OpenPopUpCreateSelection()
    {
        IsVisibleCreateSelection = true;


    }

    [RelayCommand]
    private void OpenParametersPopUp()
    {
        IsVisibleParameters = true;
    }

    [RelayCommand]
    private void ClickCancelButton()
    {
        IsVisibleAddJob = false;
        IsVisibleDeleteJob = false;
        IsVisibleCreateSelection = false;


        IsVisibleParameters = false;
    }

    [RelayCommand]
    private void AddJob()
    {
        if (string.IsNullOrWhiteSpace(JobName) || string.IsNullOrWhiteSpace(SourcePath)
           || string.IsNullOrWhiteSpace(TargetPath))
        {
            return;
        }

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
        foreach (var job in SelectedJobs.ToList())
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

        foreach (var job in SelectedJobs.ToList())
        {
            _backupService.RunBackupJob(job, IsCryptChecked);
        }
    }

    [RelayCommand]
    private void RunAllJobs()
    {
        if (Jobs == null || Jobs.Count == 0)
            return;

        foreach (var job in Jobs)
        {
            _backupService.RunBackupJob(job, IsCryptChecked);
        }
    }

    [RelayCommand]
    private void ToggleLogFileType(bool isXml)
    {
        var logFileType = isXml ? "xml" : "json";
        AppSettingsHelper.SetLogFileType(logFileType);
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
            _backupService.RunBackupJobByIndex(job, IsCryptChecked);
        }

        IsVisibleCreateSelection = false;
        SelectedJobsText = "" ;
    }

    [RelayCommand]
    private void AddExtension(string extension)
    {
        if (!string.IsNullOrWhiteSpace(extension) && !Extensions.Contains(extension))
        {
            if (!extension.StartsWith("."))
            {
                extension = "."+extension;
            }
            Extensions.Add(extension);
            NewExtension = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveExtension(string extension)
    {
        if (Extensions.Contains(extension))
        {
            Extensions.Remove(extension);
        }
    }

    [RelayCommand]
    private void AddSoftware(string software)
    {
        if (!string.IsNullOrWhiteSpace(software) && !Softwares.Contains(software))
        {
            Softwares.Add(software);
            NewSoftware = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveSoftware(string software)
    {
        if (Softwares.Contains(software))
        {
            Softwares.Remove(software);
        }
    }

    [RelayCommand]
    private void SaveExtensionsAndSoftwares()
    {
        SaveExtensions();
        SaveSoftwares();
    }

    private void SaveExtensions()
    {
        AppSettingsHelper.SetEncryptExtensions([.. Extensions]);
        IsVisibleParameters = false;
    }

    private void SaveSoftwares()
    {
        AppSettingsHelper.SetSoftwares([.. Softwares]);
        IsVisibleParameters = false;
    }

    private void LoadExtensions()
    {
        var savedExtensions = AppSettingsHelper.GetEncryptExtensions();
        Extensions = new ObservableCollection<string>(savedExtensions);
    }

    private void LoadSoftwares()
    {
        var savedSoftwares = AppSettingsHelper.GetSoftwares();
        Softwares = new ObservableCollection<string>(savedSoftwares);
    }

    private void LoadExtensionsAndSoftwares()
    {
        LoadExtensions();
        LoadSoftwares();
    }
}
