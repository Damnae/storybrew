using System.Windows.Forms;

namespace BrewLib.Util
{
    public static class ClipboardHelper
    {
        public static void SetText(string text, TextDataFormat format = TextDataFormat.Text)
            => Misc.WithRetries(() => Clipboard.SetText(text, format), 500, false);

        public static string GetText(TextDataFormat format = TextDataFormat.Text)
            => Misc.WithRetries(() => Clipboard.GetText(format), 500, false);

        public static void SetData(string format, object data)
            => Misc.WithRetries(() => Clipboard.SetData(format, data), 500, false);

        public static object GetData(string format)
            => Misc.WithRetries(() => Clipboard.GetData(format), 500, false);
    }
}
