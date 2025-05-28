using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Customer.Model;
using System.Text.Json;

namespace Customer.Services
{

    public class ReceivedData
    {
        public BackupJob JobDetails { get; set; }
        public double ProgressPercentage { get; set; }
    }
    public class WebSocketService
    {

        public event Action<BackupJob, double> ProgressUpdated;


        public Socket ConnectToServer()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress serverAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint serverEndPoint = new IPEndPoint(serverAddress, 8080);

            try
            {
                clientSocket.Connect(serverEndPoint);
                Console.WriteLine("Connect to server");
                
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error connecting to server: " + ex.Message);
                return null!;
            }

            _ = Task.Run(async () =>
            {
                while (clientSocket.Connected)
                {
                    var result = await ListenToServer(clientSocket);

                    if (result != null)
                    {
                        ProgressUpdated?.Invoke(result.Value.job, result.Value.ProgressPercentage);
                    }
                }
            });

            return clientSocket;
        }


        public async Task<(BackupJob job, double ProgressPercentage)?> ListenToServer(Socket client)
        {
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                while (client.Connected)
                {
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    }
                    catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.TimedOut)
                    {
                        return null;
                    }

                    if (bytesRead == 0)
                    {
                        return null;
                    }

                    string receivedChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    try
                    {
                        var data = JsonSerializer.Deserialize<ReceivedData>(receivedChunk);

                        if (data != null)
                        {
                            return (data.JobDetails, data.ProgressPercentage);
                        }
                        else
                        {
                            return null;
                        }
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                }
            }
            catch (SocketException)
            {
                return null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }



        public void SendMessage(Socket socket, string message)
        {
            if (socket == null || !socket.Connected)
            {
                Console.WriteLine("Socket is not connected.");
                return;
            }
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            try
            {
                socket.Send(messageBytes);
                Console.WriteLine("Message sent: " + message);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error sending message: " + ex.Message);
            }
        }

        public Socket Disconnect(Socket socket)
        {
            SendMessage(socket, "disconnect");
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            return null!;

        }
    }
}
