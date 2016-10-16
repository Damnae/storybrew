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

        public override void Write(string message) => log(message);
        public override void WriteLine(string message) => log(message);

        private void log(string message)
        {
            try
            {
                if (!Program.IsMainThread && Program.SchedulingEnabled)
                    Program.Schedule(() => File.AppendAllText(path, message + "\n"));
                else File.AppendAllText(path, message + "\n");
            }
            catch { }
        }
    }
}
