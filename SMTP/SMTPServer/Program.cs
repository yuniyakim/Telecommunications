using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            const int port = 25;
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Waiting for connections...");

            var clientNumber = 0;

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var server = new SMTPServer(client, clientNumber);
                ++clientNumber;
                var thread = new Thread(server.Run);
                thread.Start();
            }
        }
    }

    /// <summary>
    /// SMTP server
    /// </summary>
    public class SMTPServer
    {
        private TcpClient client;
        private int number;
        private int amount = 0;

        /// <summary>
        /// SMTP server constructor
        /// </summary>
        /// <param name="client">TCP client</param>
        /// <param name="number">Number of server</param>
        public SMTPServer(TcpClient client, int number)
        {
            this.client = client;
            this.number = number;
        }

        /// <summary>
        /// Runs SMTP server
        /// </summary>
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
                    if (response.ToLower() == "helo" || response.ToLower() == "mail from" || response.ToLower() == "rcpt to")
                    {
                        Write("501 Invalid argument");
                        continue;
                    }
                    if (response.StartsWith("HELO ") || response.StartsWith("helo ") || response.StartsWith("Helo"))
                    {
                        try
                        {
                            name = response.Substring(5).Trim();
                        }
                        catch
                        {
                            Write("501 Invalid argument");
                            continue;
                        }
                        Write($"250 Hello {name}");
                    }
                    else if (response.StartsWith("RCPT TO ") || response.StartsWith("rcpt to ") || response.StartsWith("Rcpt to "))
                    {
                        var recipient = "";
                        try
                        {
                            recipient = response.Substring(8).Trim();
                        }
                        catch
                        {
                            Write("501 Invalid argument");
                            continue;
                        }
                        recipients.Add(recipient);
                        Write($"250 {recipient} recipient accepted");
                    }
                    else if (response.StartsWith("MAIL FROM ") || response.StartsWith("mail from ") || response.StartsWith("Mail from "))
                    {
                        try
                        {
                            sender = response.Substring(10).Trim();
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
                            while (true)
                            {
                                try
                                {
                                    response = Read();
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
                                    data += response == Environment.NewLine ? response : response + Environment.NewLine;
                                }
                            }

                            var directory = Directory.GetCurrentDirectory() + ChangeDirectoryUp(4) + "Mail";
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }

                            var message = "";
                            message += $"From: {sender}" + Environment.NewLine;
                            message += $"Sent : {DateTime.UtcNow.ToString(new CultureInfo(CultureInfo.CurrentCulture.Name))}, UTC" + Environment.NewLine;
                            message += $"To: {string.Join("; ", recipients.ToArray())}" + Environment.NewLine + Environment.NewLine;
                            message += data;
                            using (var streamWriter = File.CreateText(directory + Path.AltDirectorySeparatorChar + $"mail{number}_{amount}.txt"))
                            {
                                streamWriter.Write(message);
                            }
                            ++amount;

                            Write("250 OK");
                        }
                    }
                    else if (response == "QUIT" || response == "quit" || response.StartsWith("Quit"))
                    {
                        Write("221 Bye");
                        quitFlag = !quitFlag;
                    }
                    else if (response.StartsWith("NOOP") || response.StartsWith("noop") || response.StartsWith("Noop"))
                    {
                        Write("250 OK");
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

        /// <summary>
        /// Goes up in the directory path
        /// </summary>
        /// <param name="amount">Amount of ups</param>
        /// <returns>Directory after going up</returns>
        private string ChangeDirectoryUp(int amount)
        {
            var directory = "";
            for (var i = 0; i < amount; ++i)
            {
                directory += Path.AltDirectorySeparatorChar + "..";
            }
            return directory + Path.AltDirectorySeparatorChar;
        }

        /// <summary>
        /// Writes message to TCP client's stream
        /// </summary>
        /// <param name="message">Message to write</param>
        private void Write(string message)
        {
            var stream = client.GetStream();
            var messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
            stream.Flush();
        }

        /// <summary>
        /// Reads data from TCP client's stream
        /// </summary>
        /// <returns>Data from stream</returns>
        private string Read()
        {
            var messageBytes = new byte[8192];
            var numberOfBytes = client.GetStream().Read(messageBytes, 0, 8192);
            var message = Encoding.UTF8.GetString(messageBytes, 0, numberOfBytes);
            return message;
        }
    }
}
