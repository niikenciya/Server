using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Server
{
    class Program
    {
        //private static Server server;
        private static string ipPattern = @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$";
        private static string portPattern = @"^\d{4,5}$";
        public static void Main(string[] args)
        {
            Console.WriteLine("Добро пожаловать в создание сервера!");
            Console.Write("Введите адрес интерфейса сервера, по умолчанию 0.0.0.0: ");
            IPAddress iPAddress = readIp();
            Console.Write("Введите порт сервера, по умолчанию 5555: ");
            ushort port = readPort();
            var server = new Server(iPAddress, port);
            server.Start();
        }
        private static IPAddress readIp()
        {
            string input = Console.ReadLine();
            if (input == "")
            {
                Console.WriteLine("0.0.0.0");
                return IPAddress.Any;
            }
            var matchCount = Regex.Matches(input, ipPattern).Count;
            if (matchCount == 1)
            {
                int[] ipBytes = input.Split('.').Select(int.Parse).ToArray();
                if (ipBytes[0] < 256 && ipBytes[1] < 256 && ipBytes[2] < 256 && ipBytes[3] < 256)
                {
                    return IPAddress.Parse(input);
                }
            }
            Console.Write("Введен некорректный адрес интерфейса, попробуйте снова: ");
            return readIp();
        }
        private static ushort readPort()
        {
            string input = Console.ReadLine();
            if (input == "")
            {
                Console.WriteLine("5555");
                return 5555;
            }
            var matchCount = Regex.Matches(input, portPattern).Count;
            if (matchCount == 1)
            {
                var port = Convert.ToInt32(input);

                if (port >= 1024 && port <= 49151)
                {
                    return Convert.ToUInt16(port);
                }
            }
            Console.Write("Порт должен находиться в диапазоне [1024-49151], попробуйте снова: ");
            return readPort();

        }
    }
}

