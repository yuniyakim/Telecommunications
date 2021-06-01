using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SMTPServer
{
    public class Program
    {
        public static void Main()
        {
            var listener = new TcpListener(IPAddress.Any, 25);
            listener.Start();
            Console.WriteLine("Waiting for connections...");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var server = new SMTPServer(client);
                var thread = new Thread(server.Run);
                thread.Start();
            }
        }
    }

    public class SMTPServer
    {
        private TcpClient client;

        public SMTPServer(TcpClient client)
        {
            this.client = client;
        }

        public void Run()
        {
            Console.WriteLine("Connection established.");
            Write("220 localhost SMTP ready");

            var message = "";
            var quitFlag = false;
            do
            {
                try
                {
                    message = Read();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }

                if (message.Length > 0)
                {
                    if (message.StartsWith("HELO") || message.StartsWith("helo"))
                    {
                        Write("250 localhost");
                    }
                    else if (message.StartsWith("RCPT TO:") || message.StartsWith("rcpt to:"))
                    {
                        Write("250 OK");
                    }
                    else if (message.StartsWith("MAIL FROM:") || message.StartsWith("mail from:"))
                    {
                        Write("250 OK");
                    }
                    else if (message.StartsWith("DATA") || message.StartsWith("data"))
                    {
                        Write("354 Enter message, ending with \".\" on a line by itself");
                        message = Read();
                        Write("250 OK");
                    }
                    else if (message == "QUIT" || message == "quit")
                    {
                        Write("221 Bye");
                        quitFlag = !quitFlag;
                    }
                    else
                    {
                        Write("500 Invalid command");
                    }
                }
            } while (!quitFlag);

            client.Close();
            Console.WriteLine("Connection closed");
        }

        private void Write(string message)
        {
            var stream = client.GetStream();
            var messageBytes = new UTF8Encoding().GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
            stream.Flush();
        }

        private string Read()
        {
            var messageBytes = new byte[8192];
            var numberOfBytes = client.GetStream().Read(messageBytes, 0, 8192);
            var message = new UTF8Encoding().GetString(messageBytes, 0, numberOfBytes);
            return message;
        }
    }
}
