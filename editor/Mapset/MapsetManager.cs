using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StorybrewEditor.Mapset
{
    public class MapsetManager : IDisposable
    {
        private string path;
        private bool logLoadingExceptions;

        private List<EditorBeatmap> beatmaps = new List<EditorBeatmap>();
        public IEnumerable<EditorBeatmap> Beatmaps => beatmaps;
        public int BeatmapCount => beatmaps.Count;

        public MapsetManager(string path, bool logLoadingExceptions)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Mapset path cannot be empty", nameof(path));

            this.path = path;
            this.logLoadingExceptions = logLoadingExceptions;

            loadBeatmaps();
            initializeMapsetWatcher();
        }

        #region Beatmaps

        private void loadBeatmaps()
        {
            if (!Directory.Exists(path))
                return;

            foreach (var beatmapPath in Directory.GetFiles(path, "*.osu", SearchOption.TopDirectoryOnly))
                try
                {
                    beatmaps.Add(EditorBeatmap.Load(beatmapPath));
                }
                catch (Exception e)
                {
                    if (logLoadingExceptions)
                        Trace.WriteLine($"Failed to load beatmap: {e}");
                    else throw e;
                }
        }

        #endregion

        #region Events

        private FileSystemWatcher fileWatcher;
        private ThrottledActionScheduler scheduler = new ThrottledActionScheduler();

        public event FileSystemEventHandler OnFileChanged;

        private void initializeMapsetWatcher()
        {
            if (!Directory.Exists(path))
                return;

            fileWatcher = new FileSystemWatcher()
            {
                Path = path,
                IncludeSubdirectories = true,
            };
            fileWatcher.Created += mapsetFileWatcher_Changed;
            fileWatcher.Changed += mapsetFileWatcher_Changed;
            fileWatcher.Renamed += mapsetFileWatcher_Changed;
            fileWatcher.Error += (sender, e) => Trace.WriteLine($"Watcher error (mapset): {e.GetException()}");
            fileWatcher.EnableRaisingEvents = true;
            Trace.WriteLine($"Watching (mapset): {path}");
        }

        private void mapsetFileWatcher_Changed(object sender, FileSystemEventArgs e)
            => scheduler.Schedule(e.FullPath, (key) =>
            {
                if (disposedValue) return;

                if (Path.GetExtension(e.Name) == ".osu")
                    Trace.WriteLine($"Watched mapset file {e.ChangeType.ToString().ToLowerInvariant()}: {e.FullPath}");

                OnFileChanged?.Invoke(sender, e);
            });

        #endregion

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    fileWatcher?.Dispose();
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
