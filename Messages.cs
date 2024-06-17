using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;


// -> клиент
// <- сервер
// 
// 
// авторизация
// 
// -> 0x01 {Username} 0x00
// <- 0x02 {ResultCode} [0-9] {ErrorText} 0x00
// <- 0x03 {ServerCaption} 0x00
// <- 0x04 user1 0x10 user2 0x10 ..... usern 0x10 0x00
// 
// 
// отправка сообщений
// 
// -> 0x05 {MsgText} 0x00
// <- 0x06 {Unixtime x64} {Username} 0x20 {MsgText} 0x00
// 
// 
// Вход
// 
// <- 0x07 {Unixtime x64} {Username} 0x00
// 
// Выход
// 
// <- 0x08 {Unixtime x64} {Username} 0x00


namespace Messages
{
    abstract class Message
    {
        public byte MsgCode { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        private byte eofByte { get; set; } = 0x00;

        public byte[] Serialize()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(MsgCode);
            bw.Write(Data);
            bw.Write(eofByte);
            return ms.ToArray();
        }

        public static byte[] GetRawData(byte[] data)
        {
            return data.Skip(1).Take(data.Length - 2).ToArray();
            // return data[1..^1].ToArray(); // [1..^1] - убираем первый и последний байт. Источник: https://stackoverflow.com/a/70672739/23765108
        }

    }

    class AuthMessage : Message
    {
        public string UserName;
        public AuthMessage(string userName)
        {
            UserName = userName;
            MsgCode = 0x01;
            Data = Encoding.UTF8.GetBytes(userName);
        }
        public static AuthMessage Deserialize(byte[] data)
        {
            return new AuthMessage(
                Encoding.UTF8.GetString(GetRawData(data))
            );
        }
    }
    class AuthResultMessage : Message
    {
        public char ResultCode;
        public string ErrorText;
        public AuthResultMessage(char resultCode, string errorText = "")
        {
            ResultCode = resultCode;
            ErrorText = errorText;
            MsgCode = 0x02;
            Data = new byte[] { (byte)resultCode };
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(errorText)).ToArray();
        }
        public static AuthResultMessage Deserialize(byte[] data)
        {
            return new AuthResultMessage(
                (char)data[0],
                UnicodeEncoding.UTF8.GetString(GetRawData(data))
            );
        }
    }
    class ServerCaptionMessage : Message
    {
        public string ServerCaption;
        public ServerCaptionMessage(string serverCaption = "")
        {
            ServerCaption = serverCaption;
            MsgCode = 0x03;
            Data = UnicodeEncoding.UTF8.GetBytes(serverCaption);
        }
        public static ServerCaptionMessage Deserialize(byte[] data)
        {
            return new ServerCaptionMessage(
                UnicodeEncoding.UTF8.GetString(GetRawData(data))
            );
        }
    }
    class UsersMessage : Message
    {
        public List<string> Users = new List<string>();
        public UsersMessage(List<string> users)
        {
            Users = users;
            MsgCode = 0x04;
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            foreach (string user in users)
            {
                bw.Write(UnicodeEncoding.UTF8.GetBytes(user));
                bw.Write(0x10);
            }
            Data = ms.ToArray();
        }
        public UsersMessage Deserialize(byte[] data)
        {
            List<string> users = new List<string>();
            data = GetRawData(data);
            int start = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x10) // Найден разделитель строк
                {
                    string username = UnicodeEncoding.UTF8.GetString(data, start, i - start);
                    users.Add(username);
                    start = i + 1; // Обновляем начало следующей строки после разделителя
                }
            }
            return new UsersMessage(users);
        }
    }
}
