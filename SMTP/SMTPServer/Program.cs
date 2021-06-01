using System;
using System.Collections.Generic;
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

            var response = "";
            var quitFlag = false;

            var name = "";
            var sender = "";
            var recipients = new List<string>();
            var data = "";

            do
            {
                try
                {
                    response = Read().Trim();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }

                if (response.Length > 0)
                {
                    if (response.StartsWith("HELO ") || response.StartsWith("helo ") || response.StartsWith("Helo"))
                    {
                        try
                        {
                            name = response.Substring(5);
                        }
                        catch
                        {
                            Write("501 Invalid argument");
                            continue;
                        }
                        Write($"250 Hello {name}");
                    }
                    else if (response.StartsWith("RCPT TO:") || response.StartsWith("rcpt to:") || response.StartsWith("Rcpt to:"))
                    {
                        var recipient = "";
                        try
                        {
                            recipient = response.Substring(8);
                        }
                        catch
                        {
                            Write("501 Invalid argument");
                            continue;
                        }
                        recipients.Add(recipient);
                        Write($"250 {recipient} recipient accepted");
                    }
                    else if (response.StartsWith("MAIL FROM:") || response.StartsWith("mail from:") || response.StartsWith("Mail from:"))
                    {
                        try
                        {
                            sender = response.Substring(10);
                        }
                        catch
                        {
                            Write("501 Invalid argument");
                            continue;
                        }
                        Write($"250 {sender} sender accepted");
                    }
                    else if (response == "DATA" || response == "data" || response.StartsWith("Data"))
                    {
                        if (sender == "" || recipients.Count == 0)
                        {
                            Write(sender == "" ? "501 Empty sender" : "501 Empty recipient");
                        }
                        else
                        {
                            Write("354 Enter message, ending with \".\" on a line by itself");
                            var message = "";
                            while (true)
                            {
                                try
                                {
                                    response = Read().Trim();
                                    Console.WriteLine(response);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    break;
                                }
                                if (response.Length > 0)
                                {
                                    if (response == ".")
                                    {
                                        break;
                                    }
                                    message += response;
                                }
                                message += "\n";
                            }
                            Write("250 OK");
                        }
                    }
                    else if (response == "QUIT" || response == "quit" || response.StartsWith("Quit"))
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
            var messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
            stream.Flush();
        }

        private string Read()
        {
            var messageBytes = new byte[8192];
            var numberOfBytes = client.GetStream().Read(messageBytes, 0, 8192);
            var message = Encoding.UTF8.GetString(messageBytes, 0, numberOfBytes);
            return message;
        }
    }
}
