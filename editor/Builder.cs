using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace StorybrewEditor
{
    public class Builder
    {
        public static void Build()
        {
            var archiveName = $"storybrew.{Program.Version.Major}.{Program.Version.Minor}.zip";
            Trace.WriteLine($"\n\nBuilding {archiveName}\n");

            var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptsDirectory = Path.GetFullPath(Path.Combine(appDirectory, "../../../scripts"));

            try
            {
                using (var stream = new FileStream(archiveName, FileMode.Create, FileAccess.ReadWrite))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    addFile(archive, "StorybrewEditor.exe", appDirectory);
                    addFile(archive, "StorybrewEditor.exe.config", appDirectory);
                    foreach (var path in Directory.EnumerateFiles(appDirectory, "*.dll", SearchOption.TopDirectoryOnly))
                        addFile(archive, path, appDirectory);
                    foreach (var path in Directory.EnumerateFiles(scriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly))
                        addFile(archive, path, scriptsDirectory, "scripts");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Build failed:\n\n{e}", Program.FullName);
                return;
            }

            Trace.WriteLine($"\nOpening {appDirectory}");
            Process.Start(appDirectory);
        }

        private static void addFile(ZipArchive archive, string path, string sourceDirectory, string targetPath = null)
        {
            path = Path.GetFullPath(path);

            var entryName = PathHelper.GetRelativePath(sourceDirectory, path);
            if (targetPath != null)
            {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);
                entryName = Path.Combine(targetPath, entryName);
            }

            Trace.WriteLine($"  Adding {path} -> {entryName}");
            var entry = archive.CreateEntryFromFile(path, entryName, CompressionLevel.Optimal);
        }
    }
}