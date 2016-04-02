using System;
using System.IO;

namespace StorybrewEditor.Mapset
{
    public class MapsetManager : IDisposable
    {
        private string path;
        private FileSystemWatcher fileWatcher;

        public event FileSystemEventHandler OnFileChanged;

        public MapsetManager(string path)
        {
            this.path = path;
            initializeMapsetWatcher();
        }

        private void initializeMapsetWatcher()
        {
            fileWatcher = new FileSystemWatcher()
            {
                Path = path,
                IncludeSubdirectories = true,
            };
            fileWatcher.Changed += mapsetFileWatcher_Changed;
            fileWatcher.Renamed += mapsetFileWatcher_Changed;
            fileWatcher.EnableRaisingEvents = true;
        }

        private void mapsetFileWatcher_Changed(object sender, FileSystemEventArgs e)
            => Program.Schedule(() =>
            {
                if (disposedValue) return;
                OnFileChanged?.Invoke(sender, e);
            });

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    fileWatcher.Dispose();
                }
                fileWatcher = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
