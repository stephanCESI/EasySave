using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Customer.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Customer.Model;   

namespace Customer.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WebSocketService _webSocketService;
        private Socket? socket;

        [ObservableProperty]
        bool isVisibleConnect;

        [ObservableProperty]
        bool isVisibleDisconnect;

        [ObservableProperty]
        private BackupJob backupJobInfo;

        [ObservableProperty]
        private double progressBarValue;


        public MainViewModel(WebSocketService webSocketService) {
            _webSocketService = webSocketService;

            _webSocketService.ProgressUpdated += OnMessageReceived;
            isVisibleConnect = true;
            isVisibleDisconnect = false;

        }

        [RelayCommand]
        private void ConnectToServer()
        {

            socket = _webSocketService.ConnectToServer();

            if (socket != null)
            {
                IsVisibleConnect = false;
                IsVisibleDisconnect = true;
                Toast.Make("Connected to server successfully.", ToastDuration.Short).Show();


            }
        }


        [RelayCommand]
        private void StopJob()
        {

            if (socket != null)
            {
                _webSocketService.SendMessage(socket, "stop");
            }

        }

        [RelayCommand]
        private void DeleteJob()
        {
            if (socket != null)
            {
                _webSocketService.SendMessage(socket, "delete");
            }
        }

        [RelayCommand]
        private void PlayJob()
        {
            if (socket != null)
            {
                _webSocketService.SendMessage(socket, "start");

            }
        }

        [RelayCommand]
        private void Disconnect()
        {
            if (socket != null)
            {
               socket =  _webSocketService.Disconnect(socket);

                if (socket == null)
                {
                    IsVisibleConnect = true;
                    IsVisibleDisconnect = false;
                    Toast.Make("Disconnect to server successfully.", ToastDuration.Short).Show();
                }
            }

        }

        private void OnMessageReceived(BackupJob job, double progress)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                backupJobInfo = job;
                progressBarValue = progress;
            });
        }
    }
}
