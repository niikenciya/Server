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
// NewMessageMsg        <- 0x06 {Unixtime x64} {Username} 0x10 {MsgText} 0x00
// 
// 
// Вход
// 
// UserEnterMsg            <- 0x07 {Unixtime x64} {Username} 0x00
// 
// Выход
// 
// UserLeaveMsg           <- 0x08 {Unixtime x64} {Username} 0x00


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
        // -> 0x01 {Username} 0x00
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
        // <- 0x02 {ResultCode(1 byte)} {ErrorText} 0x00
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
        // <- 0x03 {ServerCaption} 0x00
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
        // <- 0x04 user1 0x10 user2 0x10 ..... usern 0x10 0x00
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
        public static UsersMsg Deserialize(byte[] data)
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
        // -> 0x05 {MsgText} 0x00
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
        // <- 0x06 {Unixtime x64} {Username} 0x10 {MsgText} 0x00
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
            Data.Add(0x10);
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(text)).ToList();
        }
        public static NewMessageMsg Deserialize(byte[] data)
        {
            data = GetRawData(data);
            var unixTimeBytes = data.Take(8).ToArray();
            data = data.Skip(8).ToArray();
            string userName = "";
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x10) // Найден разделитель строк
                {
                    userName = UnicodeEncoding.UTF8.GetString(data, 0, i);
                    break;
                }
            }
            string text = UnicodeEncoding.UTF8.GetString(data.Skip(userName.Length + 1).ToArray());
            return new NewMessageMsg(text, Utils.BytesToDateTime(unixTimeBytes), userName);
        }
    }
    class UserEnterMsg : Msg
    {
        public DateTime Time;
        public string UserName;
        public UserEnterMsg(DateTime time, string userName)
        {
            // <- 0x07 {Unixtime x64} {Username} 0x00
            Time = time;
            UserName = userName;
            MsgCode = 0x07;
            Data = Utils.DateTimeToBytes(time).ToList();
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(userName)).ToList();
        }
        public static UserEnterMsg Deserialize(byte[] data)
        {
            data = GetRawData(data);
            var unixTimeBytes = data.Take(8).ToArray();
            string UserName = UnicodeEncoding.UTF8.GetString(data.Skip(8).ToArray());
            return new UserEnterMsg(Utils.BytesToDateTime(unixTimeBytes), UserName);
        }
    }
    class UserLeaveMsg : Msg
    {
        // <- 0x08 {Unixtime x64} {Username} 0x00
        public DateTime Time;
        public string UserName;
        public UserLeaveMsg(DateTime time, string userName)
        {
            Time = time;
            UserName = userName;
            MsgCode = 0x08;
            Data = Utils.DateTimeToBytes(time).ToList();
            Data = Data.Concat(UnicodeEncoding.UTF8.GetBytes(userName)).ToList();
        }
        public static UserLeaveMsg Deserialize(byte[] data)
        {
            data = GetRawData(data);
            var unixTimeBytes = data.Take(8).ToArray();
            string UserName = UnicodeEncoding.UTF8.GetString(data.Skip(8).ToArray());
            return new UserLeaveMsg(Utils.BytesToDateTime(unixTimeBytes), UserName);
        }
    }
}