using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Maui.Models;
using EasySave.Maui.Services;
using System.Collections.ObjectModel;

namespace EasySave.Maui.ViewModels;
public partial class MainViewModel : ObservableObject
{
    private readonly BackupService _backupService;

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
    private ObservableCollection<BackupJob> selectedJobs = new();

    partial void OnSelectedJobsChanged(ObservableCollection<BackupJob> value)
    {
        OnPropertyChanged(nameof(IsDeleteButtonEnabled));
    }

    public bool IsDeleteButtonEnabled => SelectedJobs.Count > 0;

    public Array BackupTypes { get; } = Enum.GetValues(typeof(BackupType));

    [ObservableProperty]
    public string selectedJobNames;

    public MainViewModel(BackupService backupService)
    {
        _backupService = backupService;
        LoadJobs();
        IsVisibleAddJob = false;
        IsVisibleDeleteJob = false;
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
    private void ClickCancelButton()
    {
        IsVisibleAddJob = false;
        IsVisibleDeleteJob = false;
    }

    [RelayCommand]
    private void AddJob()
    {
        if (string.IsNullOrWhiteSpace(JobName) || string.IsNullOrWhiteSpace(SourcePath)
           || string.IsNullOrWhiteSpace(TargetPath))
        {
            return;
        }

        _backupService.CreateBackupJob(JobName, SourcePath, TargetPath, selectedBackupType);
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
}
