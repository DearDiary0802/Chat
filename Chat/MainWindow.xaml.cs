using System.Windows;
using System.Net;
using System.Net.Sockets;

namespace Chat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IPAddress userip;
        IPAddress[] Host;
        public MainWindow()
        {
            InitializeComponent();
            Host = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            GetLocalIPAddress();
            userip = Host[ComboIPs.SelectedIndex];

            TxtMessage.IsEnabled = false;
            BtnDisconnect.IsEnabled = false;
            BtnSend.IsEnabled = false;
        }
        private Connection userConnection;
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserName.Text;
            userConnection = new Connection(userName, userip, GetMessage, DisplayMessage);
            userConnection.Connect();

            UserName.IsReadOnly = true;
            TxtMessage.IsEnabled = true;
            BtnConnect.IsEnabled = false;
            BtnDisconnect.IsEnabled = true;
            BtnSend.IsEnabled = true;
        }
        private string GetMessage()
        {
            return TxtMessage.Text;
        }
        private void DisplayMessage(string message)
        {
            TxtChat.AppendText(message);
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            userConnection.SendMessage();
            TxtMessage.Clear();

        }
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            userConnection.Disconnect();
            Close();
        }
        public void GetLocalIPAddress()
        {
            foreach (IPAddress IP in Host)
                if (IP.AddressFamily == AddressFamily.InterNetwork)
                    ComboIPs.Items.Add(IP);
            ComboIPs.SelectedIndex = ComboIPs.Items.Count - 1;
        }
    }
}
