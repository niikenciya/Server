using Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using M = Messages;

namespace Server
{
    internal class Server
    {

        private TcpListener tcpListener;
        private string caption;
        private Dictionary<string, Socket> users = new Dictionary<string, Socket>();
        public Server(IPAddress ipAddress, ushort port, string caption) {
            this.caption = caption;
            this.tcpListener = new TcpListener(ipAddress, port);
        }
        private void sendMsg(Socket soket, M.Msg msg)
        {
            var bytes = msg.Serialize();
            Thread sendThr = new Thread(() => {
                try
                {
                    soket.Send(bytes);
                }
                catch (Exception)
                {
                    Console.WriteLine("Не удалось отправить сообщение");
                }
            });
            
            sendThr.Start();
        }
        private byte[] readForFlag(Socket soket, byte flag=0x00)
        {
            var buf = new List<byte>();
            while (true)
            {
                
                byte[] codeBuf = new byte[1];
                soket.Receive(codeBuf);
                buf.Add(codeBuf[0]);
                if (codeBuf[0] == flag)
                {
                    return buf.ToArray();
                }
            }
        }
        
        public void Start() {
            try
            {
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений... ");
                listener();
            }
            catch (Exception E)
            {
                Console.WriteLine("Непредвиденная ошибка", E);
            }
        }

        private void userWorker(Socket socket) 
        {
            string userName = "";
            while (socket.Connected) {
                try
                {

                    var data = readForFlag(socket);
                    
                    switch (data[0])
                    {
                        case 0x01:
                            var resultMsg = new M.AuthResultMsg(
                                    0x01
                                    );
                            var authMsg = M.AuthMsg.Deserialize(data);
                            userName = authMsg.UserName;
                            Console.WriteLine("Попытка подключения с именем " + userName);
                            if (users.ContainsKey(userName))
                            {
                                Console.WriteLine("Отказ. Имя уже занято");
                                resultMsg = new M.AuthResultMsg(
                                    0x02,
                                    "Данное имя уже занято"
                                    );
                            }
                            if (userName.Length < 2)
                            {
                                Console.WriteLine("Отказ. Имя слишком короткое");
                                resultMsg = new M.AuthResultMsg(
                                    0x03,
                                    "Имя не может быть короче 2 символов"
                                    );
                            }
                            if (userName.Length > 25)
                            {
                                Console.WriteLine("Отказ. Имя слишком длинное");
                                resultMsg = new M.AuthResultMsg(
                                    0x04,
                                    "Имя не может быть больше 25 символов"
                                    );
                            }

                            sendMsg(socket, resultMsg);
                            if (resultMsg.ResultCode != 1)
                            {
                                Thread.Sleep(1000); // ждем секунду, пока сервер отправит ответ дабы не получить ошибку закрытого сокета в методе sendMsg
                                socket.Close();
                                return;
                            }
                            users[userName] = socket;
                            sendMsg(socket, new M.ServerCaptionMsg(caption));
                            Console.WriteLine(userName + " успешно подключился");
                            // todo разослать всем сообщение об этом великом событии

                            break;

                        case 0x05:
                            var sendChatMessageMsg = M.SendChatMessageMsg.Deserialize(data);
                            Console.WriteLine(userName + ": " + sendChatMessageMsg.Text);
                            broadcast(new M.NewMessageMsg(
                                sendChatMessageMsg.Text,
                                DateTime.Now,
                                userName
                                ));
                            break;


                        default:
                            break;
                    }
                }
                catch (Exception ex) {
                    if (ex is SocketException || ex is System.ObjectDisposedException)
                    {
                        users.Remove(userName);
                        Console.WriteLine(userName + " вышел");
                        break;
                    }
                    // todo разослать всем сообщение о выходе пользователя
                }
            }
        }

        private void broadcast(M.Msg msg)
        {
            foreach (var socket in users.Values)
            {
                sendMsg(socket, msg);
            }
            
        }
        private void listener()
        {
            try
            {
                while (true)
                {
                    var tcpSocket = tcpListener.AcceptSocket();
                    Console.WriteLine($"Входящее подключение: {tcpSocket.RemoteEndPoint}");
                    var worker = new Thread(() => userWorker(tcpSocket));
                    worker.Start();
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Непредвиденная ошибка", E);
            }
            tcpListener.Stop();
        }
        
    }
}
