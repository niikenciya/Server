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
        private List<string> users = new List<string>();
        public Server(IPAddress ipAddress, ushort port, string caption) {
            this.caption = caption;
            this.tcpListener = new TcpListener(ipAddress, port);
        }
        private void sendMsg(Socket soket, M.Msg msg)
        {
            var bytes = msg.Serialize();
            soket.Send(bytes);
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
            while (true) {
                var data = readForFlag(socket);

                switch (data[0])
                {
                    case 0x01:
                        var authMsg = M.AuthMsg.Deserialize(data);
                        var userName = authMsg.UserName;
                        Console.WriteLine("Попытка подключения с именем" + userName);
                        if (users.Contains(userName))
                        {
                            sendMsg(socket, new M.AuthResultMsg(
                                0x02,
                                "Данное имя уже занято"
                                ));
                                break;
                        }
                        if (userName.Length < 2)
                        {
                            sendMsg(socket, new M.AuthResultMsg(
                                0x03,
                                "Имя не может быть короче 2 символов"
                                ));
                            break;
                        }
                        if (userName.Length > 25)
                        {
                            sendMsg(socket, new M.AuthResultMsg(
                                0x04,
                                "Имя не может быть больше 25 символов"
                                ));
                            break;
                        }
                        users.Add(userName);
                        sendMsg(socket, new M.AuthResultMsg(
                                0x01
                                ));
                        sendMsg(socket, new M.ServerCaptionMsg(caption));
                        break;



                    default:
                        break;
                }

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
