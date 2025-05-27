using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using EasySave.Maui.Models;

namespace EasySave.Maui.Services
{
    public class StateManager
    {
        private readonly string _stateFilePath;
        private List<BackupState> _states;

        public StateManager(string logDirectory)
        {
            _stateFilePath = Path.Combine(logDirectory, "state.json");
            _states = new List<BackupState>();

            if (File.Exists(_stateFilePath))
            {
                LoadState();
            }
        }

        public void UpdateState(BackupState newState)
        {
            var existingState = _states.Find(s => s.Name == newState.Name);

            if (existingState != null)
            {
                _states.Remove(existingState);
            }

            newState.LastUpdate = DateTime.Now;
            _states.Add(newState);
            SaveState();
        }

        public void ClearState(string jobName)
        {
            _states.RemoveAll(s => s.Name == jobName);
            SaveState();
        }

        private void SaveState()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_states, Formatting.Indented);
                File.WriteAllText(_stateFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde de l'état : {ex.Message}");
            }
        }

        private void LoadState()
        {
            try
            {
                string jsonContent = File.ReadAllText(_stateFilePath);
                _states = JsonConvert.DeserializeObject<List<BackupState>>(jsonContent) ?? new List<BackupState>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de l'état : {ex.Message}");
            }
        }
    }
}
