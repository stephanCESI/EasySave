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
            }
            catch(Exception ex)
            {

            }
        }

        public  Socket AcceptConnection(Socket socket)
        {
            Socket clientSocket = socket.Accept();
           
            IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint ;
            Console.WriteLine($"Client connected from {clientEndPoint?.Address} : {clientEndPoint?.Port}");
            return clientSocket;
        }

        public static void ListenToClient(Socket client)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = client.Receive(buffer);
            string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

        }
    }
}
