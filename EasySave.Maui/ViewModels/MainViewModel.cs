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
    private BackupJob? selectedJob;

    public Array BackupTypes { get; } = Enum.GetValues(typeof(BackupType));

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
    private void DeleteSelectedJob()
    {
        if (SelectedJob == null) return;

        // Supprime le job dans le service
        _backupService.DeleteBackupJobByName(SelectedJob.Name);

        // Retire le job de la liste observable
        Jobs.Remove(SelectedJob);
        SelectedJob = null;
        IsVisibleDeleteJob = false;
    }
}
