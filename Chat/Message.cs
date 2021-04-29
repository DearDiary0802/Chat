using System;
using System.Text;

namespace Chat
{
    class Message
    {
        public byte Code;
        public int Size;
        public string Text;

        public Message(byte code, int size, string text)
        {
            Code = code;
            Size = size;
            Text = text;
        }

        public Message(byte[] message)
        {
            Code = message[0];
            byte[] SizeArray = new byte[4];
            int Size;
            Array.Copy(message, 1, SizeArray, 0, 4);
            Size = BitConverter.ToInt32(SizeArray, 0);
            string stringMessage = Encoding.UTF8.GetString(message, 5, Size);
            Text = stringMessage;
        }
    }
}
