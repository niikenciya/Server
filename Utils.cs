using System;

namespace Messages
{
    static class Utils
    {
        public static byte[] DateTimeToBytes(DateTime dateTime)
        {
            var result = new byte[8];
            var unix = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
            for (int i = 0; i < 8; i++)
            {
                result[i] = (byte)(unix % 255 + 1);
                unix = unix / 255;
            }
            return result;
        }

        public static DateTime BytesToDateTime(byte[] bytes)
        {
            long unix = 0;
            for (int i = 0; i < 8; i++)
            {
                unix += (bytes[i] - 1) * (long)Math.Pow(255, i);
            }

            return DateTimeOffset.FromUnixTimeSeconds(unix).DateTime;
        }
    }
}
