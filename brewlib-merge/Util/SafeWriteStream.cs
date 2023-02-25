using System.IO;

namespace BrewLib.Util
{
    public class SafeWriteStream : FileStream
    {
        readonly string temporaryPath, path;
        bool commited, disposed;

        public SafeWriteStream(string path) : base(prepare(path), FileMode.Create)
        {
            this.path = path;
            temporaryPath = Name;
        }

        public void Commit() => commited = true;
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;

            base.Dispose(disposing);

            if (disposing && commited)
            {
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
        }
        static string prepare(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return path + ".tmp";
        }
    }
}