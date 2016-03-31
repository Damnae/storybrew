using System.Text;

namespace StorybrewEditor.Util
{
    public static class StringExtensions
    {
        private static readonly string utf8Bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        public static string StripUtf8Bom(this string s) => s.StartsWith(utf8Bom) ? s.Remove(0, utf8Bom.Length) : s;
    }
}
