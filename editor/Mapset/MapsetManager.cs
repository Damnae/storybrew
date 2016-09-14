using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewEditor.Mapset
{
    public class MapsetManager : IDisposable
    {
        private string path;

        private List<EditorBeatmap> beatmaps = new List<EditorBeatmap>();
        public IEnumerable<EditorBeatmap> Beatmaps => beatmaps;
        public int BeatmapCount => beatmaps.Count;

        public MapsetManager(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Mapset path cannot be empty", nameof(path));

            this.path = path;
            loadBeatmaps();
            initializeMapsetWatcher();
        }

        #region Beatmaps

        private void loadBeatmaps()
        {
            foreach (var beatmapPath in Directory.GetFiles(path, "*.osu", SearchOption.TopDirectoryOnly))
                beatmaps.Add(EditorBeatmap.Load(beatmapPath));
        }

        #endregion

        #region Events

        private FileSystemWatcher fileWatcher;
        private ThrottledActionScheduler scheduler = new ThrottledActionScheduler();

        public event FileSystemEventHandler OnFileChanged;

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
            => scheduler.Schedule(e.FullPath, (key) =>
            {
                if (disposedValue) return;
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
