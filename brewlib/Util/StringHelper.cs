using System.Globalization;

namespace BrewLib.Util
{
    public static class StringHelper
    {
        private static string[] sizeOrders = { "b", "kb", "mb", "gb", "tb" };

        public static string ToByteSize(double byteCount, string format = "{0:0.#} {1}")
        {
            var order = 0;
            while (byteCount >= 1024 && order < sizeOrders.Length - 1)
            {
                order++;
                byteCount /= 1024;
            }
            return string.Format(CultureInfo.InvariantCulture, format, byteCount, sizeOrders[order]);
        }
    }
}
