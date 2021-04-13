using System.Text;

namespace Chat
{
    class Message
    {
        public int Code;
        public string Text;

        public Message(int code, string text)
        {
            Code = code;
            Text = text;
        }

        public Message(byte[] message)
        {
            string stringMessage = Encoding.UTF8.GetString(message);
            Code = int.Parse(stringMessage[0].ToString());
            Text = stringMessage.Substring(1);
        }
    }
}
