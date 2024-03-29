﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StorybrewEditor.Util
{
    public class MultiFileWatcher : IDisposable
    {
        private Dictionary<string, FileSystemWatcher> folderWatchers = new Dictionary<string, FileSystemWatcher>();
        private Dictionary<string, FileSystemWatcher> recursiveFolderWatchers = new Dictionary<string, FileSystemWatcher>();
        private HashSet<string> watchedFilenames = new HashSet<string>();
        private readonly ThrottledActionScheduler scheduler = new ThrottledActionScheduler();

        public IEnumerable<string> WatchedFilenames => watchedFilenames;

        public event FileSystemEventHandler OnFileChanged;

        public void Watch(IEnumerable<string> filenames)
        {
            foreach (var filename in filenames)
                Watch(filename);
        }

        public void Watch(string filename)
        {
            filename = Path.GetFullPath(filename);
            var directoryPath = Path.GetDirectoryName(filename);

            lock (watchedFilenames)
            {
                if (watchedFilenames.Contains(filename)) return;
                watchedFilenames.Add(filename);
            }

            if (Directory.Exists(directoryPath))
            {
                // The folder containing the file to watch exists, 
                // only watch that folder

                if (!folderWatchers.TryGetValue(directoryPath, out FileSystemWatcher watcher))
                {
                    folderWatchers.Add(directoryPath, watcher = new FileSystemWatcher()
                    {
                        Path = directoryPath,
                        IncludeSubdirectories = false,
                    });
                    watcher.Created += watcher_Changed;
                    watcher.Changed += watcher_Changed;
                    watcher.Renamed += watcher_Changed;
                    watcher.Error += (sender, e) => Trace.WriteLine($"Watcher error: {e.GetException()}");
                    watcher.EnableRaisingEvents = true;
                    Trace.WriteLine($"Watching folder: {directoryPath}");
                }
                Trace.WriteLine($"Watching file: {filename}");
            }
            else
            {
                // The folder containing the file to watch does not exist, 
                // find a parent to watch subfolders from

                var parentDirectory = Directory.GetParent(directoryPath);
                while (parentDirectory != null && !parentDirectory.Exists)
                    parentDirectory = Directory.GetParent(parentDirectory.FullName);

                if (parentDirectory != null && parentDirectory != parentDirectory.Root)
                {
                    var parentDirectoryPath = parentDirectory.ToString();
                    
                    if (!recursiveFolderWatchers.TryGetValue(parentDirectoryPath, out FileSystemWatcher watcher))
                    {
                        recursiveFolderWatchers.Add(parentDirectoryPath, watcher = new FileSystemWatcher()
                        {
                            Path = parentDirectoryPath,
                            IncludeSubdirectories = true,
                        });
                        watcher.Created += watcher_Changed;
                        watcher.Changed += watcher_Changed;
                        watcher.Renamed += watcher_Changed;
                        watcher.Error += (sender, e) => Trace.WriteLine($"Watcher error: {e.GetException()}");
                        watcher.EnableRaisingEvents = true;
                        Trace.WriteLine($"Watching folder and subfolders: {parentDirectoryPath}");
                    }
                }
                else Trace.WriteLine($"Cannot watch file: {filename}, directory does not exist");
            }
        }

        public void Clear()
        {
            foreach (var folderWatcher in folderWatchers.Values)
                folderWatcher.Dispose();
            folderWatchers.Clear();

            foreach (var folderWatcher in recursiveFolderWatchers.Values)
                folderWatcher.Dispose();
            recursiveFolderWatchers.Clear();

            lock (watchedFilenames)
                watchedFilenames.Clear();
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Trace.WriteLine($"File {e.ChangeType.ToString().ToLowerInvariant()}: {e.FullPath}");
            scheduler.Schedule(e.FullPath, (key) =>
                {
                    if (disposedValue) return;

                    lock (watchedFilenames)
                        if (!watchedFilenames.Contains(e.FullPath)) return;

                    Trace.WriteLine($"Watched file {e.ChangeType.ToString().ToLowerInvariant()}: {e.FullPath}");
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
                OnFileChanged = null;
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
