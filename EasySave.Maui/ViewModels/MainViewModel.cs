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
    private string destinationPath;

    [ObservableProperty]
    private string backupType;

    public MainViewModel(BackupService backupService)
    {
        _backupService = backupService;
        LoadJobs();
    }

    public void LoadJobs()
    {
        Jobs.Clear();
        foreach (var job in _backupService.GetJobs())
            Jobs.Add(job);
    }

    [RelayCommand]
    private void AddJob()
    {
        if (string.IsNullOrWhiteSpace(JobName) || string.IsNullOrWhiteSpace(SourcePath)
           || string.IsNullOrWhiteSpace(DestinationPath) || string.IsNullOrWhiteSpace(BackupType))
        {
            // Affiche un message d'erreur ou log
            return;
        }

        if (!Enum.TryParse<BackupType>(BackupType, true, out var parsedType))
        {
            // Erreur : type invalide
            return;
        }

        _backupService.CreateBackupJob(JobName, SourcePath, DestinationPath, parsedType);

        // Recharge la liste après ajout
        LoadJobs();

        // Réinitialise les champs
        JobName = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        BackupType = string.Empty;
    }
}
