using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Customer.Services
{
    internal class Customer
    {
        private static Socket ConnectToServer()
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

            return clientSocket;
        }


        private static void ListenToServer(Socket client)
        {

        }

        private static void Disconnect(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

        }
    }
}
