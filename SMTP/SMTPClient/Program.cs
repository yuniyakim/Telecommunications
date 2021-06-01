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

                var responseBytes = new byte[256];
                var stream = client.GetStream();
                Console.WriteLine("Connection established.");

                var message = "";
                var numberOfBytes = 0;

                while (true)
                {
                    numberOfBytes = stream.Read(responseBytes, 0, responseBytes.Length);
                    var response = Encoding.UTF8.GetString(responseBytes, 0, numberOfBytes);
                    Console.WriteLine(response);

                    if (response.StartsWith("221"))
                    {
                        break;
                    }
                    else if (response.StartsWith("354"))
                    {
                        numberOfBytes = 0;
                        var data = "";
                        do
                        {
                            data = Console.ReadLine();
                            var dataBytes = Encoding.UTF8.GetBytes(data);
                            stream.Write(dataBytes, 0, dataBytes.Length);
                            stream.Flush();
                        }
                        while (data.Trim() != ".");
                        continue;
                    }

                    message = Console.ReadLine().Trim();
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                    stream.Flush();
                }

                numberOfBytes = stream.Read(responseBytes, 0, responseBytes.Length);
                Console.WriteLine(Encoding.UTF8.GetString(responseBytes, 0, numberOfBytes));

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