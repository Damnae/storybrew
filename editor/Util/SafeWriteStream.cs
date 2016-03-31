using System.IO;

namespace StorybrewEditor.Util
{
    public class SafeWriteStream : FileStream
    {
        private string temporaryPath;
        private string path;
        private bool disposed;

        public SafeWriteStream(string path)
            : base(prepare(path), FileMode.Create)
        {
            this.path = path;
            temporaryPath = Name;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;

            base.Dispose(disposing);

            if (disposing)
            {
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
        }

        private static string prepare(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return path + ".tmp";
        }
    }
}
