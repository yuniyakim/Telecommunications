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

                var data = new byte[128];
                var response = new StringBuilder();
                var stream = client.GetStream();
                Console.WriteLine("Connection established.");

                var message = "";
                var numberOfBytes = 0;
                do
                {
                    do
                    {
                        numberOfBytes = stream.Read(data, 0, data.Length);
                    }
                    while (numberOfBytes == 0);
                    Console.WriteLine(new UTF8Encoding().GetString(data, 0, numberOfBytes));
                    message = Console.ReadLine();
                    var messageBytes = new UTF8Encoding().GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                    stream.Flush();
                    numberOfBytes = 0;
                } while (message != "QUIT" && message != "quit");

                numberOfBytes = stream.Read(data, 0, data.Length);
                Console.WriteLine(new UTF8Encoding().GetString(data, 0, numberOfBytes));

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