using System.Text;
using System.Net;
using System.Net.Sockets;
using System;

namespace Chat
{
    class User
    {
        public string userName;
        public IPAddress userIP;
        private int Port;
        private TcpClient ChatUser;
        public NetworkStream Stream;
        public bool IsOnline = true;
        private readonly int READ_BUFFER = 4096;

        public User(TcpClient chatUser, int port)
        {
            ChatUser = chatUser;
            userIP = ((IPEndPoint)chatUser.Client.RemoteEndPoint).Address;
            Port = port;
            Stream = chatUser.GetStream();
        }

        public User(string UserName, IPAddress UserIP, int port)
        {
            userName = UserName;
            userIP = UserIP;
            Port = port;
        }
        public void Connect()
        {
            IPEndPoint IPEndPoint = new IPEndPoint(userIP, Port);
            ChatUser = new TcpClient();
            ChatUser.Connect(IPEndPoint);
            Stream = ChatUser.GetStream();
        }
        public void SendMessage(Message TCPMessage)
        {
            byte[] text = Encoding.UTF8.GetBytes(TCPMessage.Text);
            byte[] message = new byte[5 + text.Length];
            message[0] = TCPMessage.Code;
            Array.Copy(BitConverter.GetBytes(TCPMessage.Size), 0, message, 1, 4);
            Array.Copy(text, 0, message, 5, text.Length);
            Stream.Write(message, 0, message.Length);
        }
        public byte[] ReceiveMessage()
        {
            int size = 0, readSize = 0;
            byte[] buffer = new byte[READ_BUFFER];
            byte[] readData = new byte[READ_BUFFER];
            do
            {
                size = Stream.Read(readData, readSize, READ_BUFFER - readSize);
                Array.Copy(readData, 0, buffer, readSize, size);
                readSize += size;
            }
            while (Stream.DataAvailable && size > 0);

            return buffer;
        }
        public void Close()
        {
            IsOnline = false;
            Stream.Close();
            ChatUser.Close();
        }
    }
}
