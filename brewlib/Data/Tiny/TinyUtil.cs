namespace BrewLib.Data.Tiny
{
    public class TinyUtil
    {
        public static string EscapeString(string value)
            => value?
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

        public static string UnescapeString(string value)
            => value?
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
    }
}
