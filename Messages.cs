using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// -> клиент
// <- сервер
// 
// 
// авторизация
// 
// AuthMsg              -> 0x01 {Username} 0x00
// AuthResultMsg        <- 0x02 {ResultCode(1 byte)} {ErrorText} 0x00
// ServerCaptionMsg     <- 0x03 {ServerCaption} 0x00
// UsersMsg             <- 0x04 user1 0x10 user2 0x10 ..... usern 0x10 0x00
// 
// 
// отправка сообщений
// 
// SendChatMessageMsg   -> 0x05 {MsgText} 0x00
// NewMessageMsg        <- 0x06 {Unixtime x64} {Username} 0x20 {MsgText} 0x00
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

    abstract class Msg
    {
        public byte MsgCode { get; set; }
        public List<byte> Data { get; set; } = new List<byte>();
        private byte eofByte { get; set; } = 0x00;

        public byte[] Serialize()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(MsgCode);
            bw.Write(Data.ToArray());
            bw.Write(eofByte);
            return ms.ToArray();
        }

        public static byte[] GetRawData(byte[] data)
        {
            return data.Skip(1).Take(data.Length - 2).ToArray();
        }

    }

    class AuthMsg : Msg
    {
        public string UserName;
        public AuthMsg(string userName)
        {
            UserName = userName;
            MsgCode = 0x01;
            Data = Encoding.UTF8.GetBytes(userName).ToList();
        }
        public static AuthMsg Deserialize(byte[] data)
        {
            return new AuthMsg(
                Encoding.UTF8.GetString(GetRawData(data))
            );
        }
    }
    class AuthResultMsg : Msg
    {
        public byte ResultCode;
        public string ErrorText;
        public AuthResultMsg(byte resultCode, string errorText = "")
        {
            ResultCode = resultCode;
            ErrorText = errorText;
            MsgCode = 0x02;
            Data.Add(resultCode);
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(errorText)).ToList();
        }
        public static AuthResultMsg Deserialize(byte[] data)
        {
            var rawData = GetRawData(data);
            return new AuthResultMsg(
                rawData[0],
                UnicodeEncoding.UTF8.GetString(rawData.Skip(1).ToArray())
            );
        }
    }
    class ServerCaptionMsg : Msg
    {
        public string ServerCaption;
        public ServerCaptionMsg(string serverCaption = "")
        {
            ServerCaption = serverCaption;
            MsgCode = 0x03;
            Data = UnicodeEncoding.UTF8.GetBytes(serverCaption).ToList();
        }
        public static ServerCaptionMsg Deserialize(byte[] data)
        {
            return new ServerCaptionMsg(
                UnicodeEncoding.UTF8.GetString(GetRawData(data))
            );
        }
    }
    class UsersMsg : Msg
    {
        public List<string> Users = new List<string>();
        public UsersMsg(List<string> users)
        {
            Users = users;
            MsgCode = 0x04;
            // MemoryStream ms = new MemoryStream();
            // BinaryWriter bw = new BinaryWriter(ms);
            foreach (string user in users)
            {
                Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(user)).ToList();
                Data.Add(0x10);
            }
        }
        public UsersMsg Deserialize(byte[] data)
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
            return new UsersMsg(users);
        }
    }
    class SendChatMessageMsg : Msg
    {
        public string Text;
        public SendChatMessageMsg(string text)
        {
            Text = text;
            MsgCode = 0x05;
            Data = UnicodeEncoding.UTF8.GetBytes(text).ToList();
        }
        public static SendChatMessageMsg Deserialize(byte[] data)
        {
            return new SendChatMessageMsg(
                UnicodeEncoding.UTF8.GetString(GetRawData(data))
            );
        }
    }
    class NewMessageMsg : Msg
    {
        public string Text;
        public DateTime Time;
        public string UserName;
        public NewMessageMsg(string text, DateTime time, string userName)
        {
            Text = text;
            Time = time;
            UserName = userName;
            MsgCode = 0x06;
            Data = Utils.DateTimeToBytes(time).ToList();
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(userName)).ToList();
            Data.Add(0x20);
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(text)).ToList();
        }
    }
    class UserEnter : Msg
    {
        public DateTime Time;
        public string UserName;
        public UserEnter(string text, DateTime time, string userName)
        {
            Time = time;
            UserName = userName;
            MsgCode = 0x07;
            Data = Utils.DateTimeToBytes(time).ToList();
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(userName)).ToList();
        }
    }

    class UserLeave : Msg
    {
        public DateTime Time;
        public string UserName;
        public UserLeave(string text, DateTime time, string userName)
        {
            Time = time;
            UserName = userName;
            MsgCode = 0x08;
            Data = Utils.DateTimeToBytes(time).ToList();
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(userName)).ToList();
        }
    }
}
