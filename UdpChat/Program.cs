using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UdpMessageConsoleApp
{
    //Variant 3
    internal static class Program
    {
        private const string RemoteHost = "127.0.0.1";
        private static int _remotePortToSendMessages;
        private static int _localPortToListenMessages;
        private static int _messageNum = 1;

        public static void Main()
        {
            InitConnectionStrings();

            var receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start();

            SendMessage();
        }

        private static void InitConnectionStrings()
        {
            Console.WriteLine("Enter the listening port: ");
            _localPortToListenMessages = int.Parse(Console.ReadLine() ?? string.Empty);

            Console.WriteLine("Enter the connection port: ");
            _remotePortToSendMessages = int.Parse(Console.ReadLine() ?? string.Empty);
            Console.Clear();
        }

        private static void SendMessage()
        {
            var udpClientSender = new UdpClient();

            try
            {
                while (true)
                {
                    var messageToSend = Console.ReadLine();

                    if (SendIsOk(udpClientSender, messageToSend))
                    {
                        ClearCurrentConsoleLine();
                        Console.WriteLine($"{_messageNum} | You: {messageToSend}");
                        _messageNum++;
                    }
                    else
                    {
                        Console.WriteLine("Error was acquired while sending this message. Please, try again.");
                    }
                }
            }
            finally
            {
                udpClientSender.Close();
            }
        }

        private static void ReceiveMessage()
        {
            var udpClientReciever = new UdpClient(_localPortToListenMessages);
            IPEndPoint remoteAdressIp = null;

            try
            {
                while (true)
                {
                    var data = udpClientReciever.Receive(ref remoteAdressIp);
                    var recievedMessage = Encoding.ASCII.GetString(data);

                    Console.WriteLine($"{_messageNum} | Interlocutor: {recievedMessage}");
                    Console.SetCursorPosition(0, _messageNum);
                    _messageNum++;
                }
            }
            finally
            {
                udpClientReciever.Close();
            }
        }

        private static bool SendIsOk(UdpClient udpClientSender, string messageToSend)
        {
            try
            {
                var data = Encoding.ASCII.GetBytes(messageToSend);
                udpClientSender.Send(data, data.Length, RemoteHost, _remotePortToSendMessages);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ClearCurrentConsoleLine()
        {
            var currentLineCursor = Console.CursorTop - 1;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (var i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
