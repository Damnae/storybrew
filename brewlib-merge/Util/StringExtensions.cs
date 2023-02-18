using System.Globalization;
using System.Text;

namespace BrewLib.Util
{
    public static class StringExtensions
    {
        static readonly string utf8Bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        public static string StripUtf8Bom(this string s) => s.StartsWith(utf8Bom) ? s.Remove(0, utf8Bom.Length) : s;

        public static string PrettifyDashSeparated(this string s) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s).Replace('-', ' ');
    }
}