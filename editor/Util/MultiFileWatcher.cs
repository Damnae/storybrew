using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewEditor.Util
{
    public class MultiFileWatcher : IDisposable
    {
        private Dictionary<string, FileSystemWatcher> folderWatchers = new Dictionary<string, FileSystemWatcher>();
        private HashSet<string> watchedFilenames = new HashSet<string>();

        public event FileSystemEventHandler OnFileChanged;

        public void Watch(string filename)
        {
            filename = Path.GetFullPath(filename);
            if (watchedFilenames.Contains(filename)) return;
            watchedFilenames.Add(filename);

            var directoryPath = Path.GetDirectoryName(filename);

            FileSystemWatcher watcher;
            if (!folderWatchers.TryGetValue(directoryPath, out watcher))
            {
                folderWatchers.Add(directoryPath, watcher = new FileSystemWatcher()
                {
                    Path = directoryPath,
                    IncludeSubdirectories = false,
                });
                watcher.Changed += watcher_Changed;
                watcher.Renamed += watcher_Changed;
                watcher.EnableRaisingEvents = true;
            }
        }

        public void Clear()
        {
            foreach (var folderWatcher in folderWatchers.Values)
                folderWatcher.Dispose();
            folderWatchers.Clear();
            watchedFilenames.Clear();
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Program.Schedule(() =>
            {
                if (disposedValue) return;
                if (!watchedFilenames.Contains(e.FullPath)) return;

                OnFileChanged?.Invoke(sender, e);
            });
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Clear();
                }
                folderWatchers = null;
                watchedFilenames = null;
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
