using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpChat
{
    //Variant 3
    internal static class Program
    {
        private const string RemoteHost = "127.0.0.1";
        private static int _remotePortToSendMessages;
        private static int _localPortToListenMessages;

        private static string _currentUserName;
        private static string _interlocutorUserName;

        private static string _separator = "|||||";


        private class Message
        {
            public string UserName { get; }
            public DateTime SendTime { get; }
            public string Text { get; }
            public bool IsMine { get; }
            public bool IsConfirmed { get; set; }
            public string Key { get; }

            public Message(DateTime sendTime, string userName, string text, bool isMine, string key)
            {
                UserName = userName;
                SendTime = sendTime;
                Text = text;
                IsMine = isMine;
                IsConfirmed = false;
                Key = key;
            }
        }

        private static List<Message> Messages = new List<Message>();


        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Insert your nickname:");
            _currentUserName = Console.ReadLine();

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
                    var messageToSend = currentTime + _separator + $"{_currentUserName}" + _separator + message;

                    var key = GenerateKey(messageToSend);
                    
                    var data = Encoding.UTF8.GetBytes(messageToSend);
                    udpClientSender.Send(data, data.Length, RemoteHost, _remotePortToSendMessages);
                    
                    Messages.Add(new Message(Convert.ToDateTime(currentTime), _currentUserName, message, true, key));
                    UpdateMessages();
                }
            }
            finally
            {
                udpClientSender.Close();
            }
        }

        private static async void ReceiveMessage()
        {
            var udpClientReciever = new UdpClient(_localPortToListenMessages);
            IPEndPoint remoteAdressIp = null;

            try
            {
                while (true)
                {
                    var data = udpClientReciever.Receive(ref remoteAdressIp);
                    var recievedMessage = Encoding.UTF8.GetString(data);

                    var messageWithThatKey = Messages.FirstOrDefault(message => message.Key == recievedMessage && message.IsConfirmed != true);
                    if (messageWithThatKey != null)
                    {
                        messageWithThatKey.IsConfirmed = true;
                        UpdateMessages();   
                    }
                    else
                    {
                        var timeUserMessage = recievedMessage.Split(_separator);
                        var currentTime = Convert.ToDateTime(timeUserMessage[0]);
                        _interlocutorUserName = timeUserMessage[1];
                        var message = timeUserMessage[2];

                        var key = GenerateKey(recievedMessage);
                        Messages.Add(new Message(currentTime, _interlocutorUserName, message, false, key));
                        UpdateMessages();

                        await ReturnOkResult(udpClientReciever, key);
                    }
                }
            }
            finally
            {
                udpClientReciever.Close();
            }
        }

        private static async Task ReturnOkResult(UdpClient client, string key)
        {
            var data = Encoding.UTF8.GetBytes(key);
            await client.SendAsync(data, data.Length, RemoteHost, _remotePortToSendMessages);
        }

        private static string GenerateKey(string message)
        {
            var key = string.Empty;
            var messageStringBuilder = new StringBuilder(message);
            
            for (var i = 0; i < messageStringBuilder.Length; i += 2)
            {
                key = new string(messageStringBuilder[i] + key);
            }

            key = new string(key + "|OK|");

            return key;
        }

        private static void UpdateMessages()
        {
            Console.Clear();
            var sortedMessages = Messages.OrderBy(message => message.SendTime);
            foreach (var message in sortedMessages)
            {
                if (message.IsMine)
                {
                    string isConfirmedMessage;
                    if ((DateTime.Now - message.SendTime).TotalMinutes > 5)
                    {
                        isConfirmedMessage = " (message lost)";
                    }
                    else
                    {
                        isConfirmedMessage = message.IsConfirmed ? " (delivered)" : " (undefined)";
                    }
                    Console.WriteLine($"{_currentUserName}: " + message.Text + isConfirmedMessage);
                }
                else
                {
                    Console.WriteLine($"{_interlocutorUserName}: " + message.Text);
                }
            }
        }
    }
}