using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Maui.Models;

namespace EasySave.Maui.Services
{
    public class WebSocketService
    {
        private Socket? listenerSocket;
        private CancellationTokenSource? _serverCancellation;


        private readonly ConcurrentDictionary<Socket, CancellationTokenSource> _connectedClients = new();

        public void StartServer()
        {
            try
            {
                listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenerSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
                listenerSocket.Listen(10);
                _serverCancellation = new CancellationTokenSource();

                Console.WriteLine("Serveur démarré sur le port 8080...");

                _ = Task.Run(async () =>
                {
                    while (!_serverCancellation.Token.IsCancellationRequested)
                    {
                        await AcceptConnection(_serverCancellation.Token);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du démarrage du serveur : {ex.Message}");
            }
        }

        private async Task AcceptConnection(CancellationToken serverToken)
        {
            Socket clientSocket = await listenerSocket.AcceptAsync(serverToken);
            var clientTokenSource = new CancellationTokenSource();

            _connectedClients.TryAdd(clientSocket, clientTokenSource);
            Console.WriteLine($"Client connecté : {clientSocket.RemoteEndPoint}");

            _ = Task.Run(() => ListenToClient(clientSocket, clientTokenSource.Token));
        }

        private async Task ListenToClient(Socket client, CancellationToken token)
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (!token.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, token);
                    if (bytesRead == 0)
                    {
                        Disconnect(client);
                        return;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Message reçu de {client.RemoteEndPoint}: {message}");

                    switch (message.Trim().ToLowerInvariant())
                    {
                        case "play":
                            // À implémenter
                            break;

                        case "stop":
                            Console.WriteLine("Commande : stop");
                            break;

                        case "delete":
                            Console.WriteLine("Commande : delete");
                            break;

                        case "disconnect":
                            Disconnect(client);
                            return;

                        default:
                            Console.WriteLine("Commande inconnue.");
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Écoute annulée pour {client.RemoteEndPoint}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur avec {client.RemoteEndPoint} : {ex.Message}");
                Disconnect(client);
            }
        }

        public void SendJob(Socket client, BackupJob job, double progress)
        {
            if (!_connectedClients.ContainsKey(client) || !client.Connected)
            {
                Console.WriteLine($"Client {client.RemoteEndPoint} non trouvé ou déconnecté.");
                return;
            }

            try
            {
                var dataToSend = new
                {
                    JobDetails = job,
                    ProgressPercentage = progress
                };

                string jsonString = JsonSerializer.Serialize(dataToSend);
                byte[] byteData = Encoding.UTF8.GetBytes(jsonString);
                client.Send(byteData);

                Console.WriteLine($"Données envoyées à {client.RemoteEndPoint} : {jsonString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi à {client.RemoteEndPoint} : {ex.Message}");
            }
        }

        public void BroadcastJob(BackupJob job, double progress)
        {
            foreach (var client in _connectedClients.Keys)
            {
                SendJob(client, job, progress);
            }
        }

        public void Disconnect(Socket client)
        {
            if (_connectedClients.TryRemove(client, out var tokenSource))
            {
                try
                {
                    tokenSource.Cancel();
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    Console.WriteLine($"Client {client.RemoteEndPoint} déconnecté.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la déconnexion de {client.RemoteEndPoint} : {ex.Message}");
                }
            }
        }

        public void StopServer()
        {
            _serverCancellation?.Cancel();

            foreach (var client in _connectedClients.Keys)
            {
                Disconnect(client);
            }

            listenerSocket?.Close();
            Console.WriteLine("Serveur arrêté.");
        }
    }
}
