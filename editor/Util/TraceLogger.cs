using System.Diagnostics;
using System.IO;

namespace StorybrewEditor.Util
{
    public class TraceLogger : TraceListener
    {
        private string path;

        public TraceLogger(string path)
        {
            this.path = path;
            Trace.Listeners.Add(this);
        }

        public override void Write(string message)
            => File.AppendAllText(path, message + "\n");

        public override void WriteLine(string message)
            => File.AppendAllText(path, message + "\n");
    }
}
