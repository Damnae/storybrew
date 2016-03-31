using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StorybrewEditor.Util
{
    public static class HashHelper
    {
        public static string GetMd5(string value)
            => GetMd5(Encoding.UTF8.GetBytes(value));

        public static string GetMd5(byte[] data)
        {
            using (var md5 = MD5.Create())
                data = md5.ComputeHash(data);

            char[] characters = new char[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
                data[i].ToString("x2", CultureInfo.InvariantCulture.NumberFormat).CopyTo(0, characters, i * 2, 2);

            return new string(characters);
        }

        public static byte[] GetFileMd5Bytes(string path)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(path))
                return md5.ComputeHash(stream);
        }
    }
}
