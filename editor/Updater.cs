using BrewLib.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace StorybrewEditor
{
    public static class Updater
    {
        private static string[] ignoredPaths = { ".vscode/", "cache/", "logs/", "settings.cfg" };
        private static string[] readOnlyPaths = { "scripts/" };

        public const string UpdateArchivePath = "cache/net/update";
        public const string UpdateFolderPath = "cache/update";
        public const string FirstRunPath = "firstrun";

        private static Version readOnlyVersion = new Version(1, 8);

        public static void OpenLastestReleasePage()
            => Process.Start($"https://github.com/{Program.Repository}/releases/latest");

        public static void Update(string destinationFolder, Version fromVersion)
        {
            Trace.WriteLine($"Updating from version {fromVersion} to {Program.Version}");
            var updaterPath = Assembly.GetEntryAssembly().Location;
            var sourceFolder = Path.GetDirectoryName(updaterPath);
            try
            {
                replaceFiles(sourceFolder, destinationFolder, fromVersion);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to replace files: {e}");
                MessageBox.Show($"Update failed, please update manually.\n\n{e}", Program.FullName);
                OpenLastestReleasePage();
                Program.Report("updatefail", e);
                return;
            }

            try
            {
                updateData(destinationFolder, fromVersion);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to update data: {e}");
                MessageBox.Show($"Failed to update data.\n\n{e}", Program.FullName);
                Program.Report("updatefail", e);
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

        public static void NotifyEditorRun()
        {
            if (File.Exists(FirstRunPath))
            {
                File.Delete(FirstRunPath);
                firstRun();
            }

            if (File.Exists(UpdateArchivePath))
                Misc.WithRetries(() => File.Delete(UpdateArchivePath), canThrow: false);
            if (Directory.Exists(UpdateFolderPath))
                Misc.WithRetries(() => Directory.Delete(UpdateFolderPath, true), canThrow: false);
        }

        private static void updateData(string destinationFolder, Version fromVersion)
        {
            var settings = new Settings(Path.Combine(destinationFolder, Settings.DefaultPath));
            if (fromVersion < new Version(1, 46))
                settings.UseRoslyn.Set(false);
            settings.Save();
        }

        private static void firstRun()
        {
            Trace.WriteLine("First run\n");

            var localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            foreach (var exeFilename in Directory.GetFiles(localPath, "*.exe_", SearchOption.AllDirectories))
            {
                var newFilename = Path.ChangeExtension(exeFilename, ".exe");
                Trace.WriteLine($"Renaming {exeFilename} to {newFilename}");
                Misc.WithRetries(() => File.Move(exeFilename, newFilename), canThrow: false);
            }

            foreach (var scriptFilename in Directory.GetFiles("scripts", "*.cs", SearchOption.TopDirectoryOnly))
                File.SetAttributes(scriptFilename, FileAttributes.ReadOnly);
        }

        private static void replaceFiles(string sourceFolder, string destinationFolder, Version fromVersion)
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
                if (Path.GetExtension(destinationFilename) == ".exe_")
                    destinationFilename = Path.ChangeExtension(destinationFilename, ".exe");

                Trace.WriteLine($"  Copying {relativeFilename} to {destinationFilename}");
                replaceFile(sourceFilename, destinationFilename, readOnly, fromVersion);
            }
        }

        private static void replaceFile(string sourceFilename, string destinationFilename, bool readOnly, Version fromVersion)
        {
            var destinationFolder = Path.GetDirectoryName(destinationFilename);
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            if (readOnly && File.Exists(destinationFilename))
            {
                var attributes = File.GetAttributes(destinationFilename);
                if (!attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    // Don't update files that became readonly when coming from a version that didn't have them 
                    if (fromVersion < readOnlyVersion) return;

                    Trace.WriteLine($"  Creating backup for {destinationFilename}");
                    var backupFilename = destinationFilename + $".{DateTime.UtcNow.Ticks}.bak";
                    File.Move(destinationFilename, backupFilename);
                }
                else File.SetAttributes(destinationFilename, attributes & ~FileAttributes.ReadOnly);
            }

            Misc.WithRetries(() => File.Copy(sourceFilename, destinationFilename, true), 5000);
            if (readOnly) File.SetAttributes(destinationFilename, FileAttributes.ReadOnly);
        }

        private static bool matchFilter(string filename, string[] filters)
        {
            foreach (var filter in filters)
                if (filename.StartsWith(filter))
                    return true;
            return false;
        }
    }
}
