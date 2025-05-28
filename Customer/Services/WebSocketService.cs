using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Customer.Services
{
    public class WebSocketService
    {
        public Socket ConnectToServer()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress serverAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint serverEndPoint = new IPEndPoint(serverAddress, 8080);

            try
            {
                clientSocket.Connect(serverEndPoint);
                Console.WriteLine("Connect to server");
                Task.Run(() => ListenToServer(clientSocket));
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error connecting to server: " + ex.Message);
                return null!;
            }



            return clientSocket;
        }


        public async Task ListenToServer(Socket client)
        {

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
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            return null!;

        }
    }
}
