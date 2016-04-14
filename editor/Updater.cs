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
                replaceFiles(sourceFolder, destinationFolder, ignoredPaths);
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
            Process.Start(processPath);
        }

        private static void replaceFiles(string sourceFolder, string destinationFolder, string[] ignorePaths)
        {
            Trace.WriteLine($"\nCopying files from {sourceFolder} to {destinationFolder}");
            foreach (var sourceFilename in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                var relativeFilename = PathHelper.GetRelativePath(sourceFolder, sourceFilename);

                var ignoreFile = false;
                foreach (var ignorePath in ignorePaths)
                {
                    if (relativeFilename.StartsWith(ignorePath))
                    {
                        Trace.WriteLine($"  Ignoring {relativeFilename}, matches {ignorePath}");
                        ignoreFile = true;
                        break;
                    }
                }
                if (ignoreFile) continue;

                var destinationFilename = Path.Combine(destinationFolder, relativeFilename);
                Trace.WriteLine($"  Copying {relativeFilename} to {destinationFilename}");
                replaceFile(sourceFilename, destinationFilename);
            }
        }

        private static void replaceFile(string sourceFilename, string destinationFilename, int timeout = 5000)
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    File.Copy(sourceFilename, destinationFilename, true);
                    return;
                }
                catch
                {
                    attempts++;
                    var delay = 200;
                    if (attempts * delay > timeout) throw;

                    Trace.WriteLine($"      Waiting for {destinationFilename} {attempts}/{timeout / delay}");
                    Thread.Sleep(delay);
                }
            }
        }
    }
}
