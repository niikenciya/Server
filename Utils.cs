using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    static class Utils
    {
        public static byte[] DateTimeToBytes(DateTime dateTime)
        {
            var unix = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
            return BitConverter.GetBytes(unix);
        }

        public static DateTime BytesToDateTime(byte[] bytes)
        {
            var unix = BitConverter.ToInt64(bytes, 0);
            return DateTimeOffset.FromUnixTimeSeconds(unix).DateTime;
        }
    }
}
