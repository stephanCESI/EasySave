using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Maui.Localizations;
using EasySave.Maui.Models;
using EasySave.Maui.Services;
using EasySave.Maui.Utils;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Diagnostics;

namespace EasySave.Maui.ViewModels;
public partial class MainViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly WebSocketService _webSocketService;
    private readonly LocalizationService _localizationService;
    private readonly Dictionary<string, CancellationTokenSource> _jobCancellationTokens = new();


    public ObservableCollection<BackupJob> Jobs { get; } = new();

    public ObservableCollection<BackupJob> ActiveJobs { get; } = new();

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
    private string newExtension;

    [ObservableProperty]
    private string newSoftware;

    [ObservableProperty]
    private string newPriorityExtension;

    [ObservableProperty]
    private string newMaxFileSize;

    [ObservableProperty]
    private bool isCryptChecked;

    [ObservableProperty]
    private double progressBarValue;

    [ObservableProperty]
    private ObservableCollection<string> extensions = new();

    [ObservableProperty]
    private ObservableCollection<string> softwares = new();

    [ObservableProperty]
    private ObservableCollection<string> priorityExtensions = new();

    [ObservableProperty]
    private ObservableCollection<string> maxFileSizes = new();

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
                Toast.Make($"LogFileType mis à jour : {logFileType}", ToastDuration.Short).Show();

                
            }
        }
    }

    public string CurrentLogFormat
    {
        get
        {
            string formatKey = IsXmlLog ? "xml" : "json";
            string logFormat = _localizationService.GetLocalizedString(formatKey);
            return _localizationService.GetLocalizedString("LogFormat", logFormat);
        }
    }

    [ObservableProperty]
    private string addButtonText;
    [ObservableProperty]
    private string deleteJobButtonText;
    [ObservableProperty]
    private string executeAllButtonText;
    [ObservableProperty]
    private string executeSelectedButtonText;
    [ObservableProperty]
    private string createSelectiondButtonText;
    [ObservableProperty]
    private string cryptButtonText;
    [ObservableProperty]
    private string parametersButtonText;

    [ObservableProperty]
    private string numberTab;
    [ObservableProperty]
    private string nameTab;
    [ObservableProperty]
    private string sourcePathTab;
    [ObservableProperty]
    private string destinationPathTab;
    [ObservableProperty]
    private string backupTypeTab;
    [ObservableProperty]
    private string lastRunTab;
    [ObservableProperty]
    private string softwaresText;
    [ObservableProperty]
    private string cancelButtonText;
    [ObservableProperty]
    private string confirmButtonText;
    [ObservableProperty]
    private string createSelectionText;
    [ObservableProperty]
    private string createSelectionExample1;
    [ObservableProperty]
    private string createSelectionExample2;
    [ObservableProperty]
    private string changeLanguage;
    [ObservableProperty]
    private string changeLogFormat;
    

    private void UpdateTexts()
    {
        AddButtonText = _localizationService.GetLocalizedString("addJob");
        DeleteJobButtonText = _localizationService.GetLocalizedString("deleteJob");
        ExecuteAllButtonText = _localizationService.GetLocalizedString("executeAll");
        ExecuteSelectedButtonText = _localizationService.GetLocalizedString("executeSelected");
        CreateSelectiondButtonText = _localizationService.GetLocalizedString("createSelection");
        CryptButtonText = _localizationService.GetLocalizedString("crypt");
        ParametersButtonText = _localizationService.GetLocalizedString("parameters");

        NumberTab = _localizationService.GetLocalizedString("numberJob");
        NameTab = _localizationService.GetLocalizedString("nameJob");
        SourcePathTab = _localizationService.GetLocalizedString("sourcePathJob");
        DestinationPathTab = _localizationService.GetLocalizedString("destinationPathJob");
        BackupTypeTab = _localizationService.GetLocalizedString("backupTypeJob");
        LastRunTab = _localizationService.GetLocalizedString("lastRunJob");
        SoftwaresText = _localizationService.GetLocalizedString("softwares");

        CancelButtonText = _localizationService.GetLocalizedString("cancel");
        ConfirmButtonText = _localizationService.GetLocalizedString("confirm");

        CreateSelectionText = _localizationService.GetLocalizedString("createSelectionText");

        CreateSelectionExample1 = _localizationService.GetLocalizedString("createSelectionExample1");
        CreateSelectionExample2 = _localizationService.GetLocalizedString("createSelectionExample2");
        ChangeLanguage = _localizationService.GetLocalizedString("changeLanguage");
        ChangeLogFormat = _localizationService.GetLocalizedString("changeLogFormat");
    }

    partial void OnIsFrenchChanged(bool value)
    {
        string language = value ? "fr" : "en";
        _localizationService.SetLanguage(language);
        UpdateTexts();
        OnPropertyChanged(nameof(IsFrench));
        
        Toast.Make($"Langue : {(value ? "Français" : "Anglais")}", ToastDuration.Short).Show();

    }

    [ObservableProperty]
    public string selectedJobNames;

    public MainViewModel(BackupService backupService, LocalizationService localizationService , WebSocketService webSocketService)
    {
        _backupService = backupService;
        _webSocketService = webSocketService;   

        _localizationService = localizationService;
        LoadJobs();
        LoadParameters();
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

    [ObservableProperty]
    private BackupJob selectedJob;
    [RelayCommand]
    private void PauseJob()
    {
        if (SelectedJobs != null && SelectedJobs.Any())
        {
            foreach (var job in SelectedJobs.Where(j => j.IsActive))
            {
                _backupService.RequestUserPauseJob(job.Name);
            }
            Toast.Make("Jobs sélectionnés mis en pause.", ToastDuration.Short).Show();
        }
        else
        {
            Toast.Make("Aucun job actif sélectionné pour mettre en pause.", ToastDuration.Short).Show();
        }
    }

    [RelayCommand]
    private void RestartJob()
    {
        if (SelectedJobs != null && SelectedJobs.Any())
        {
            foreach (var job in SelectedJobs.Where(j => j.IsActive))
            {
                _backupService.RequestUserResumeJob(job.Name);
            }
            Toast.Make("Jobs sélectionnés repris.", ToastDuration.Short).Show();
        }
        else
        {
            Toast.Make("Aucun job actif sélectionné pour reprendre.", ToastDuration.Short).Show();
        }
    }

    [RelayCommand]
    private void StopJob()
    {
        if (SelectedJobs != null && SelectedJobs.Any())
        {
            foreach (var job in SelectedJobs.Where(j => j.IsActive))
            {
                _backupService.RequestUserResumeJob(job.Name);
            }
            Toast.Make("Jobs sélectionnés repris.", ToastDuration.Short).Show();
        }
        else
        {
            Toast.Make("Aucun job actif sélectionné pour reprendre.", ToastDuration.Short).Show();
        }
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
            Toast.Make($"Certain champs sont vide", ToastDuration.Short).Show();
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
    private async Task RunSelectedJobs()
    {
        if (SelectedJobs == null || SelectedJobs.Count == 0)
            return;

        var tasks = new List<Task>();
        var progresses = new Dictionary<BackupJob, double>();

        Device.BeginInvokeOnMainThread(() =>
        {
            foreach (var job in SelectedJobs)
            {
                if (!ActiveJobs.Contains(job))
                    ActiveJobs.Add(job);
            }
        });

        foreach (var job in SelectedJobs.ToList())
        {
            var cts = new CancellationTokenSource();
            _jobCancellationTokens[job.Name] = cts; // Stocker le token
            var progress = new Progress<double>(value =>
            {
                lock (progresses)
                {
                    progresses[job] = value;
                    ProgressBarValue = progresses.Values.Average();
                }
            });

            _webSocketService.BroadcastJob(job, 0);

            var task = Task.Run(async () =>
            {
                await _backupService.RunBackupJobAsync(job, IsCryptChecked, cts.Token, progress);
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Device.BeginInvokeOnMainThread(() =>
        {
            foreach (var job in SelectedJobs)
            {
                ActiveJobs.Remove(job);
                _jobCancellationTokens.Remove(job.Name); // Nettoyer le token
            }
            ProgressBarValue = 0;
        });
    }


    [RelayCommand]
    private async Task RunAllJobs()
    {
        if (Jobs == null || Jobs.Count == 0)
            return;

        var tasks = new List<Task>();
        var progresses = new Dictionary<BackupJob, double>();

        Device.BeginInvokeOnMainThread(() =>
        {
            foreach (var job in Jobs)
            {
                if (!ActiveJobs.Contains(job))
                    ActiveJobs.Add(job);
            }
        });

        foreach (var job in Jobs)
        {
            var cts = new CancellationTokenSource();
            var progress = new Progress<double>(value =>
            {
                lock (progresses)
                {
                    progresses[job] = value;
                    ProgressBarValue = progresses.Values.Average();
                }
            });

            _webSocketService.BroadcastJob(job, 0); 

            var task = Task.Run(async () =>
            {
                // Appel corrigé : CancellationToken est le 3ème argument, IProgress<double> est le 4ème
                await _backupService.RunBackupJobAsync(job, IsCryptChecked, cts.Token, progress);
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Device.BeginInvokeOnMainThread(() =>
        {
            foreach (var job in Jobs)
            {
                ActiveJobs.Remove(job);
            }
            ProgressBarValue = 0;
        });
    }



    [RelayCommand]
    private void ToggleLogFileType(bool isXml)
    {
        var logFileType = isXml ? "xml" : "json";
        AppSettingsHelper.SetLogFileType(logFileType);
        Toast.Make($"Fichier de type {(isXml ? "XML" : "JSON")}", ToastDuration.Short).Show();

    }

    [RelayCommand]
    private async Task CreateSelectionJobs()
    {
        
        var selectedJobsIndices = new List<int>();

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
                        selectedJobsIndices.Add(i);
                }
            }
            else if (int.TryParse(part, out int number))
            {
                selectedJobsIndices.Add(number);
            }
        }

        var tasks = new List<Task>();
        var progresses = new Dictionary<BackupJob, double>();
        var selectedJobs = new List<BackupJob>();

        foreach (var index in selectedJobsIndices)
        {
            BackupJob job = null;
            if (index > 0 && index <= Jobs.Count)
            {
                job = Jobs[index - 1];
            }

            if (job == null)
                continue;

            selectedJobs.Add(job);
        }


        IsVisibleCreateSelection = false;
        SelectedJobsText = "";

        Device.BeginInvokeOnMainThread(() =>
        {
            foreach (var job in selectedJobs)
            {
                if (!ActiveJobs.Contains(job))
                    ActiveJobs.Add(job);
            }
        });

        foreach (var job in selectedJobs)
        {
            var cts = new CancellationTokenSource();
            var progress = new Progress<double>(value =>
            {
                lock (progresses)
                {
                    progresses[job] = value;
                    ProgressBarValue = progresses.Values.Average();
                }
            });

            var task = Task.Run(async () => // Rendre la lambda async pour pouvoir await RunBackupJobAsync
            {
                // Appel corrigé : CancellationToken est le 3ème argument, IProgress<double> est le 4ème
                await _backupService.RunBackupJobAsync(job, IsCryptChecked, cts.Token, progress);
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Device.BeginInvokeOnMainThread(() =>
        {
            foreach (var job in selectedJobs)
            {
                ActiveJobs.Remove(job);
            }
            ProgressBarValue = 0;
        });

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
    private void AddPriorityExtension(string priorityExtension)
    {
        if (!string.IsNullOrWhiteSpace(priorityExtension) && !PriorityExtensions.Contains(priorityExtension))
        {
            if (!priorityExtension.StartsWith("."))
            {
                priorityExtension = "." + priorityExtension;
            }
            PriorityExtensions.Add(priorityExtension);
            NewPriorityExtension = string.Empty;
        }
    }

    [RelayCommand]
    private void RemovePriorityExtension(string priorityExtension)
    {
        if (PriorityExtensions.Contains(priorityExtension))
        {
            PriorityExtensions.Remove(priorityExtension);
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
    private void AddMaxFileSize(string maxFileSize)
    {
        if (!string.IsNullOrWhiteSpace(maxFileSize) && !MaxFileSizes.Contains(maxFileSize))
        {
            MaxFileSizes.Add(maxFileSize);
            NewMaxFileSize = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveMaxFileSize(string maxFileSize)
    {
        if (MaxFileSizes.Contains(maxFileSize))
        {
            MaxFileSizes.Remove(maxFileSize);
        }
    }

    [RelayCommand]
    private void SaveParameters()
    {
        SaveExtensions();
        SaveSoftwares();
        SavePriorityExtensions();
        SaveMaxFileSizes();
        IsVisibleParameters = false;
    }

    private void SaveExtensions()
    {
        AppSettingsHelper.SetEncryptExtensions([.. Extensions]);
    }

    private void SaveSoftwares()
    {
        AppSettingsHelper.SetSoftwares([.. Softwares]);
    }

    private void SavePriorityExtensions()
    {
        AppSettingsHelper.SetPriorityExtensions([.. PriorityExtensions]);
    }

    private void SaveMaxFileSizes()
    {
        AppSettingsHelper.SetMaxFileSizes([.. MaxFileSizes]);
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
    private void LoadPriorityExtensions()
    {
        var savedExtensions = AppSettingsHelper.GetPriorityExtensions();
        PriorityExtensions = new ObservableCollection<string>(savedExtensions);
    }

    private void LoadFileMaxSizes()
    {
        var savedSoftwares = AppSettingsHelper.GetMaxFileSizes();
        MaxFileSizes = new ObservableCollection<string>(savedSoftwares);
    }


    private void LoadParameters()
    {
        LoadExtensions();
        LoadSoftwares();
        LoadPriorityExtensions();
        LoadFileMaxSizes();
    }
}
