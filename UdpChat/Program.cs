using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UdpChat
{
    //Variant 3
    internal static class Program
    {
        private const string RemoteHost = "127.0.0.1";
        private static int _remotePortToSendMessages;
        private static int _localPortToListenMessages;

        private static string currentUserName;
        private static string interlocutorUserName;

        private static DateTime _lastSendedMessageTime = DateTime.Now;
        private static DateTime _lastRecievedMessageTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));


        private class Message
        {
            public string UserName { get; }
            public DateTime SendTime { get; }
            public string Text { get; }
            public bool IsMine { get; }

            public Message(DateTime sendTime, string userName, string text, bool isMine)
            {
                SendTime = sendTime;
                UserName = userName;
                Text = text;
                IsMine = isMine;
            }
        }

        private static List<Message> Messages = new List<Message>();


        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Insert your nickname:");
            currentUserName = Console.ReadLine();

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
                    var message = Console.ReadLine();
                    var currentTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                    var messageToSend = currentTime + "|||||" + $"{currentUserName}" + "|||||" + message;
                    _lastSendedMessageTime = DateTime.Now;

                    if (SendIsOk(udpClientSender, messageToSend))
                    {
                        Messages.Add(new Message(Convert.ToDateTime(currentTime), currentUserName, message, true));
                        UpdateMessages();
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
                    var recievedMessage = Encoding.UTF8.GetString(data);

                    if (recievedMessage == "OK:200")
                    {
                        _lastRecievedMessageTime = DateTime.Now;
                    }
                    else
                    {
                        var timeUserMessage = recievedMessage.Split("|||||");
                        var currentTime = Convert.ToDateTime(timeUserMessage[0]);
                        interlocutorUserName = timeUserMessage[1];
                        var message = timeUserMessage[2];

                        Messages.Add(new Message(currentTime, interlocutorUserName, message, false));
                        UpdateMessages();

                        ReturnOkResult(udpClientReciever);
                    }
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
                var data = Encoding.UTF8.GetBytes(messageToSend);
                udpClientSender.Send(data, data.Length, RemoteHost, _remotePortToSendMessages);

                Thread.Sleep(75);

                return Math.Abs((_lastRecievedMessageTime - _lastSendedMessageTime).TotalSeconds) < 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ReturnOkResult(UdpClient client)
        {
            var data = Encoding.UTF8.GetBytes("OK:200");
            client.Send(data, data.Length, RemoteHost, _remotePortToSendMessages);
        }

        private static void UpdateMessages()
        {
            Console.Clear();
            var sortedMessages = Messages.OrderBy(message => message.SendTime);
            foreach (var message in sortedMessages)
            {
                var isMineMessage = message.IsMine ? $"{currentUserName}:" : $"{interlocutorUserName}:";
                Console.WriteLine(isMineMessage + message.Text);
            }
        }
    }
}