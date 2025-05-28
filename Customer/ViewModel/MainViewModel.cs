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

namespace Customer.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WebSocketService _webSocketService;
        private Socket? socket;

        [ObservableProperty]
        private ObservableCollection<string> messages = new ObservableCollection<string>();

        [ObservableProperty]
        bool isVisibleConnect;

        [ObservableProperty]
        bool isVisibleDisconnect;

        public MainViewModel(WebSocketService webSocketService) {
            _webSocketService = webSocketService;

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
                Messages.Add("Connected to server successfully.");
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
                }
            }

        }
    }
}
