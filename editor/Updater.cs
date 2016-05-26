using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace StorybrewEditor
{
    public static class Updater
    {
        private static string[] ignoredPaths = { ".vscode/", "cache/", "logs/", "settings.cfg" };
        private static string[] readOnlyPaths = { "scripts/" };

        public const string UpdateArchivePath = "cache/net/update";
        public const string UpdateFolderPath = "cache/update";

        public static void OpenLastestReleasePage()
            => Process.Start($"https://github.com/{Program.Repository}/releases/latest");

        public static void Update(string destinationFolder, Version fromVersion)
        {
            Trace.WriteLine($"Updating from version {fromVersion} to {Program.Version}");
            var updaterPath = Assembly.GetEntryAssembly().Location;
            var sourceFolder = Path.GetDirectoryName(updaterPath);
            try
            {
                replaceFiles(sourceFolder, destinationFolder);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to replace files: {e}");
                MessageBox.Show($"Update failed, please update manually.\n\n{e}", Program.FullName);
                OpenLastestReleasePage();
                return;
            }

            // Start the updated process
            var relativeProcessPath = PathHelper.GetRelativePath(sourceFolder, updaterPath);
            var processPath = Path.Combine(destinationFolder, relativeProcessPath);

            Trace.WriteLine($"\nUpdate complete, starting {processPath}");
            Process.Start(new ProcessStartInfo()
            {
                FileName = processPath,
                WorkingDirectory = destinationFolder,
            });
        }

        public static void Cleanup()
        {
            if (File.Exists(UpdateArchivePath))
                withRetries(() => File.Delete(UpdateArchivePath));
            if (Directory.Exists(UpdateFolderPath))
                withRetries(() => Directory.Delete(UpdateFolderPath, true));
        }

        private static void replaceFiles(string sourceFolder, string destinationFolder)
        {
            Trace.WriteLine($"\nCopying files from {sourceFolder} to {destinationFolder}");
            foreach (var sourceFilename in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                var relativeFilename = PathHelper.GetRelativePath(sourceFolder, sourceFilename);

                if (matchFilter(relativeFilename, ignoredPaths))
                {
                    Trace.WriteLine($"  Ignoring {relativeFilename}");
                    continue;
                }
                var readOnly = matchFilter(relativeFilename, readOnlyPaths);

                var destinationFilename = Path.Combine(destinationFolder, relativeFilename);
                Trace.WriteLine($"  Copying {relativeFilename} to {destinationFilename}");
                replaceFile(sourceFilename, destinationFilename, readOnly);
            }
        }

        private static void replaceFile(string sourceFilename, string destinationFilename, bool readOnly)
        {
            var destinationFolder = Path.GetDirectoryName(destinationFilename);
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            if (readOnly && File.Exists(destinationFilename))
            {
                var attributes = File.GetAttributes(destinationFilename);
                if (!attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    Trace.WriteLine($"  Creating backup for {destinationFilename}");
                    File.Move(destinationFilename, destinationFilename + ".bak");
                }
                else File.SetAttributes(destinationFilename, attributes & ~FileAttributes.ReadOnly);
            }

            withRetries(() => File.Copy(sourceFilename, destinationFilename, true), 5000);
            if (readOnly) File.SetAttributes(destinationFilename, FileAttributes.ReadOnly);
        }

        private static bool matchFilter(string filename, string[] filters)
        {
            foreach (var filter in filters)
                if (filename.StartsWith(filter))
                    return true;
            return false;
        }

        private static void withRetries(Action action, int timeout = 2000)
        {
            var sleepTime = 0;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch
                {
                    if (sleepTime >= timeout) throw;

                    var retryDelay = timeout / 10;
                    sleepTime += retryDelay;
                    Thread.Sleep(retryDelay);
                }
            }
        }
    }
}
