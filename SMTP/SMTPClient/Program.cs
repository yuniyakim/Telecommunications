using System;
using System.Net.Sockets;
using System.Text;

namespace SMTPClient
{
    public class Program
    {
        private const int port = 25;
        private const string server = "localhost";

        public static void Main()
        {
            try
            {
                Console.WriteLine("Establishing connection...");
                var client = new TcpClient();
                client.Connect(server, port);

                var response = new byte[256];
                var stream = client.GetStream();
                Console.WriteLine("Connection established.");

                var message = "";
                var numberOfBytes = 0;
                do
                {
                    do
                    {
                        numberOfBytes = stream.Read(response, 0, response.Length);
                    }
                    while (numberOfBytes == 0);
                    Console.WriteLine(Encoding.UTF8.GetString(response, 0, numberOfBytes));
                    message = Console.ReadLine();
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                    stream.Flush();
                    numberOfBytes = 0;
                } while (message != "QUIT" && message != "quit");

                numberOfBytes = stream.Read(response, 0, response.Length);
                Console.WriteLine(Encoding.UTF8.GetString(response, 0, numberOfBytes));

                stream.Close();
                client.Close();
                Console.WriteLine("Connection closed.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}