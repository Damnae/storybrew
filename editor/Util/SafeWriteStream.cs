using System;
using System.IO;

namespace StorybrewEditor.Util
{
    public class SafeWriteStream : FileStream
    {
        private string temporaryPath;
        private string path;
        private bool commited;
        private bool disposed;

        public SafeWriteStream(string path)
            : base(prepare(path), FileMode.Create)
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
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
        }

        private static string prepare(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return path + ".tmp";
        }
    }
}
