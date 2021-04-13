using System.Text;
using System.Net;
using System.Net.Sockets;

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
            byte[] message = Encoding.UTF8.GetBytes(TCPMessage.Code + TCPMessage.Text);
            Stream.Write(message, 0, message.Length);
        }
        public byte[] ReceiveMessage()
        {
            StringBuilder message = new StringBuilder();
            byte[] buffer = new byte[256];
            do
            {
                int size = Stream.Read(buffer, 0, buffer.Length);
                message.Append(Encoding.UTF8.GetString(buffer, 0, size));
            }
            while (Stream.DataAvailable);

            return Encoding.UTF8.GetBytes(message.ToString());
        }
        public void Close()
        {
            IsOnline = false;
            Stream.Close();
            ChatUser.Close();
        }
    }
}
