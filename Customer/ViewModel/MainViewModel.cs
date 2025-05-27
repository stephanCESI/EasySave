using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        [ObservableProperty]
        private ObservableCollection<string> messages = new ObservableCollection<string>();

        [ObservableProperty]
        private string connect;

        [ObservableProperty]
        private string consoleLog;

        public MainViewModel(WebSocketService webSocketService) {
            _webSocketService = webSocketService;

        }

        [RelayCommand]
        private void ConnectToServer()
        {

            var socket = _webSocketService.ConnectToServer();

            if (socket != null)
            {
                Messages.Add("Connected to server");
                WebSocketService.ListenToServer(socket);
            }
            else
            {
                Messages.Add("Failed to connect to server");
            }
        }
    }
}
