using System;
using System.IO;

namespace BrewLib.Util
{
    public class SafeDirectoryWriter : IDisposable
    {
        private readonly string targetDirectory;
        private readonly string tempDirectory;
        private readonly string backupDirectory;
        private bool committed;

        public SafeDirectoryWriter(string targetDirectory)
        {
            this.targetDirectory = targetDirectory;
            tempDirectory = targetDirectory + ".tmp";
            backupDirectory = targetDirectory + ".bak";

            // Clear temporary directory
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
            Directory.CreateDirectory(tempDirectory);
        }

        public string GetPath(string path)
            => Path.Combine(tempDirectory, path);

        public void Commit()
            => committed = true;

        public void Dispose()
        {
            if (committed)
            {
                // Switch temp and target directories
                if (Directory.Exists(targetDirectory))
                {
                    if (Directory.Exists(backupDirectory))
                        Directory.Delete(backupDirectory, true);
                    Directory.Move(targetDirectory, backupDirectory);
                }
                Directory.Move(tempDirectory, targetDirectory);
                if (Directory.Exists(backupDirectory))
                    Directory.Delete(backupDirectory, true);
            }
            else
            {
                // Something failed, cleanup temporary directory
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);
            }
        }
    }
}
