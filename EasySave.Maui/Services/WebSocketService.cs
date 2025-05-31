using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;




namespace EasySave.Maui.Services
{
    public class WebSocketService
    {
        public  void StartServer()
        {
            try
            {
                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
                serverSocket.Listen(10);
                _ = Task.Run(() => { AcceptConnection(serverSocket); });

            }
            catch(Exception ex)
            {

            }
        }

        public async Task  AcceptConnection(Socket socket)
        {
            Socket clientSocket = socket.Accept();
            IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint ;
            _ = Task.Run(() => { ListenToClient(socket); });

        public void Disconnect(Socket client)
        {
            //if (_connectedClients.TryRemove(client, out var tokenSource))
            //{
            //    try
            //    {
            //        tokenSource.Cancel();
            //        client.Shutdown(SocketShutdown.Both);
            //        client.Close();
            //        Console.WriteLine($"Client {client.RemoteEndPoint} déconnecté.");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"Erreur lors de la déconnexion de {client.RemoteEndPoint} : {ex.Message}");
            //    }
            //}
        }

        public async Task ListenToClient(Socket client)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = client.Receive(buffer);
            string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine(message); 

        }
    }
}
