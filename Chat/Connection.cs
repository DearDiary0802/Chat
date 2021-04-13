using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Threading;

namespace Chat
{
    delegate string GetDelegate();
    delegate void DisplayDelegate(string message);
    delegate void ClearScreen();

    class Connection
    {
        public const int SEND_PORT = 50000;
        public const int RECEIVE_PORT = 50001;
        public const int TCP_PORT = 50002;

        public static object _locker = new object();

        public const int MESSAGE = 1;
        public const int NAME_TRANSMISSION = 2;
        public const int USER_DISCONNECT = 3;
        public const int GET_HISTORY = 4;
        public const int SEND_HISTORY = 5;

        private string userName;
        private IPAddress userIP;
        private GetDelegate get;
        private DisplayDelegate display;
        private ClearScreen clearScreen;
        private static List<User> Users = new List<User>();
        private StringBuilder history;
        private SynchronizationContext synchronizationContext;

        private bool ShouldUDPListen;
        private bool ShouldTCPListen;

        public Connection(string user, IPAddress userip, GetDelegate getDel, DisplayDelegate displayDel, ClearScreen ClearScreen)
        {
            userName = user;
            userIP = userip;
            get = getDel;
            display = displayDel;
            history = new StringBuilder();
            synchronizationContext = SynchronizationContext.Current;
            clearScreen = ClearScreen;
        }
        public void Connect()
        {
            try
            {
                IPAddress BroadcastIP = IPAddress.Parse(userIP.ToString().Substring(0, userIP.ToString().LastIndexOf('.') + 1) + "255");

                Thread listenUdp = new Thread(() => ListenToUDP());
                listenUdp.Start();

                Thread listenTcpTask = new Thread(() => ListenToTCP());
                listenTcpTask.Start();

                SendBroadcastPackage(BroadcastIP);
            }
            catch
            {
                Disconnect();
            }
        }
        public void SendBroadcastPackage(IPAddress BroadcastIP)
        {
            IPEndPoint sourceEP = new IPEndPoint(userIP, SEND_PORT);
            IPEndPoint destinationEP = new IPEndPoint(BroadcastIP, RECEIVE_PORT);

            UdpClient udpSender = new UdpClient(sourceEP) { EnableBroadcast = true };
            byte[] messageBytes = Encoding.UTF8.GetBytes(userName);

            try
            {
                udpSender.Send(messageBytes, messageBytes.Length, destinationEP);

                display($"You [{userIP}] ({DateTime.Now}) joined the chat.\n");
                history.Append($"{userName} [{userIP}] ({DateTime.Now}) joined the chat.\n");
            }
            catch
            {
                MessageBox.Show("Can't send the information about the new user.", "Error!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                udpSender.Close();
            }
        }
        private void ListenToUDP()
        {
            IPEndPoint localEP = new IPEndPoint(userIP, RECEIVE_PORT);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, SEND_PORT);

            UdpClient udpReceiver = new UdpClient(localEP);

            ShouldUDPListen = true;
            while (ShouldUDPListen)
            {
                byte[] message = udpReceiver.Receive(ref remoteEP);
                string userName = Encoding.UTF8.GetString(message);

                User newUser = new User(userName, remoteEP.Address, TCP_PORT);

                if (Users.Find(x => x.userIP.ToString() == remoteEP.Address.ToString()) == null && userIP.ToString() != remoteEP.Address.ToString())
                {
                    newUser.Connect();

                    Message tcpMessage = new Message(NAME_TRANSMISSION, this.userName);
                    newUser.SendMessage(tcpMessage);

                    lock (Connection._locker)
                    {
                        Users.Add(newUser);
                    }
                    Thread thread = new Thread(() => ListenUser(newUser));
                    thread.Start();

                    synchronizationContext.Post(
                        delegate
                        {
                            display($"{newUser.userName} [{newUser.userIP}] ({DateTime.Now}) joined the chat.\n");
                            history.Append($"{newUser.userName} [{newUser.userIP}] ({DateTime.Now}) joined the chat.\n");
                        }, null);
                }
            }

            udpReceiver.Close();
        }

        private void ListenToTCP()
        {
            TcpListener tcpListener = new TcpListener(userIP, TCP_PORT);
            tcpListener.Start();

            ShouldTCPListen = true;
            while (ShouldTCPListen)
            {
                if (tcpListener.Pending())
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    User newUser = new User(tcpClient, TCP_PORT);

                    if (Users.Find(x => x.userIP.ToString() == newUser.userIP.ToString()) == null && userIP.ToString() != newUser.userIP.ToString())
                    {
                        lock (Connection._locker)
                        {
                            Users.Add(newUser);
                        }
                        Thread thread = new Thread(() => ListenUser(newUser));
                        thread.Start();
                    }
                }
            }

            tcpListener.Stop();
        }

        public void Disconnect()
        {
            ShouldUDPListen = false;
            ShouldTCPListen = false;

            string message = $"{userName} [{userIP}] ({DateTime.Now}) disconnected.\n";
            display(message);
            history.Append(message);
            Message tcpMessage = new Message(USER_DISCONNECT, message);

            foreach (User user in Users)
            {
                try
                {
                    user.SendMessage(tcpMessage);
                }
                catch
                {
                    MessageBox.Show($"Can't send the message to {user.userName} about disconnecting.",
                        "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    user.Close();
                }
            }

            Users.Clear();
        }
        private void ListenUser(User user)
        {
            while (user.IsOnline)
            {
                if (user.Stream.DataAvailable)
                {
                    byte[] message = user.ReceiveMessage();
                    Message tcpMessage = new Message(message);

                    switch (tcpMessage.Code)
                    {
                        case NAME_TRANSMISSION:
                            user.userName = tcpMessage.Text;

                            GetHistory(SEND_HISTORY, user);

                            break;

                        case MESSAGE:
                            synchronizationContext.Post(delegate
                            {
                                string messageChat =
                                    $"{user.userName} [{user.userIP}] ({DateTime.Now}): {tcpMessage.Text}\n";
                                display(messageChat);
                                history.Append(messageChat);
                            }, null);

                            break;

                        case SEND_HISTORY:
                            Thread.Sleep(200);
                            GetHistory(GET_HISTORY, user);
                            break;

                        case GET_HISTORY:

                            synchronizationContext.Post(delegate
                            {
                                clearScreen();
                                HistoryPreparing(display, tcpMessage.Text);
                                history.Remove(0, history.Length);
                                history.Append(tcpMessage.Text);
                            }, null);

                            break;

                        case USER_DISCONNECT:
                            user.Close();
                            Users.Remove(user);

                            synchronizationContext.Post(delegate
                            {
                                string messageChat = $"{tcpMessage.Text}";
                                history.Append(messageChat);
                                display(messageChat);
                            }, null);

                            break;
                    }
                }
            }
        }
        private void GetHistory(int code, User user)
        {
            string message = (code == SEND_HISTORY) ? string.Empty : history.ToString();
            Message tcpHistoryMessage = new Message(code, message);

            user.SendMessage(tcpHistoryMessage);
        }
        public void SendMessage()
        {
            string message = get();
            Message tcpMessage = new Message(MESSAGE, message);

            foreach (User user in Users)
            {
                try
                {
                    user.SendMessage(tcpMessage);
                }
                catch
                {
                    MessageBox.Show($"Can't send the message to {user.userName}.",
                        "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            display($"You ({DateTime.Now}): {message}\n");
            history.Append($"{userName} [{userIP}] ({DateTime.Now}): {message}\n");
        }
        public void HistoryPreparing(DisplayDelegate display, string Text)
        {
            int HistoryCount = 0, AllLength = Text.Length;
            string MyHistory = "", History = Text;
            string Message;
            while (History != "")
            {
                Message = History.Substring(0, History.IndexOf('\n') + 1);
                HistoryCount += History.IndexOf('\n');
                History = History.Remove(0, History.IndexOf('\n') + 1);
                if (Message.Contains(userIP.ToString()))
                {
                    Message = Message.Remove(0, Message.IndexOf(userIP.ToString()) - 1);
                    Message = Message.Insert(0, "You");

                }
                MyHistory += Message;
            }
            display(MyHistory);
        }
    }
}
