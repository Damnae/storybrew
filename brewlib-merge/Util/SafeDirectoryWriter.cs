using System;
using System.Collections.Generic;
using System.IO;

namespace BrewLib.Util
{
    public class SafeDirectoryWriter : IDisposable
    {
        readonly List<string> paths = new List<string>();
        readonly string targetDirectory, tempDirectory, backupDirectory;
        bool committed;

        public SafeDirectoryWriter(string targetDirectory)
        {
            this.targetDirectory = targetDirectory;
            tempDirectory = targetDirectory + ".tmp";
            backupDirectory = targetDirectory + ".bak";

            // Clear temporary directory
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
            Directory.CreateDirectory(tempDirectory);
        }

        public string GetPath(string path)
        {
            var fullpath = Path.Combine(tempDirectory, path);
            paths.Add(fullpath);
            return fullpath;
        }
        public void Commit(bool checkPaths = true)
        {
            if (checkPaths)
            {
                if (paths.Count == 0) throw new InvalidOperationException($"No file path requested");
                foreach (var path in paths)
                {
                    var file = new FileInfo(path);

                    if (!file.Exists) throw new InvalidOperationException($"File path requested but not created: {path}");
                    if (file.Length == 0) throw new InvalidOperationException($"File path requested but is empty: {path}");
                }
            }

            committed = true;
        }
        public void Dispose()
        {
            if (committed)
            {
                // Switch temp and target directories
                if (Directory.Exists(targetDirectory))
                {
                    if (Directory.Exists(backupDirectory)) Directory.Delete(backupDirectory, true);
                    Directory.Move(targetDirectory, backupDirectory);
                }
                Directory.Move(tempDirectory, targetDirectory);
                if (Directory.Exists(backupDirectory)) Directory.Delete(backupDirectory, true);
            }
            else
            {
                if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
            }
        }
    }
}