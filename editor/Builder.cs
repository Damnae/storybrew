using BrewLib.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace StorybrewEditor
{
    public class Builder
    {
        private static string mainExecutablePath = "StorybrewEditor.exe";
        private static string[] ignoredPaths = { };

        public static void Build()
        {
            var archiveName = $"storybrew.{Program.Version.Major}.{Program.Version.Minor}.zip";
            var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                buildReleaseZip(archiveName, appDirectory);
            }
            catch (Exception e)
            {
                MessageBox.Show($"\nBuild failed:\n\n{e}", Program.FullName);
                return;
            }

            try
            {
                testUpdate(archiveName);
            }
            catch (Exception e)
            {
                MessageBox.Show($"\nUpdate test failed:\n\n{e}", Program.FullName);
                return;
            }

            Trace.WriteLine($"\nOpening {appDirectory}");
            Process.Start(appDirectory);
        }

        private static void buildReleaseZip(string archiveName, string appDirectory)
        {
            Trace.WriteLine($"\n\nBuilding {archiveName}\n");

            var scriptsDirectory = Path.GetFullPath(Path.Combine(appDirectory, "../../../scripts"));

            using (var stream = new FileStream(archiveName, FileMode.Create, FileAccess.ReadWrite))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                addFile(archive, mainExecutablePath, appDirectory);
                addFile(archive, "StorybrewEditor.exe.config", appDirectory);
                foreach (var path in Directory.EnumerateFiles(appDirectory, "*.dll", SearchOption.TopDirectoryOnly))
                    addFile(archive, path, appDirectory);

                // Roslyn (white-listed, most files seem useless)
                addFile(archive, "bin/roslyn/csc.exe", appDirectory);
                addFile(archive, "bin/roslyn/csc.exe.config", appDirectory);
                addFile(archive, "bin/roslyn/csc.rsp", appDirectory);
                addFile(archive, "bin/roslyn/Microsoft.CodeAnalysis.CSharp.dll", appDirectory);
                addFile(archive, "bin/roslyn/Microsoft.CodeAnalysis.dll", appDirectory);
                addFile(archive, "bin/roslyn/System.Reflection.Metadata.dll", appDirectory);

                // Scripts
                foreach (var path in Directory.EnumerateFiles(scriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly))
                    addFile(archive, path, scriptsDirectory, "scripts");

                archive.CreateEntry(Updater.FirstRunPath);
            }
        }

        private static void testUpdate(string archiveName)
        {
            var previousVersion = $"{Program.Version.Major}.{Program.Version.Minor - 1}";
            var previousArchiveName = $"storybrew.{previousVersion}.zip";
            if (!File.Exists(previousArchiveName))
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("user-agent", Program.Name);
                    webClient.DownloadFile($"https://github.com/{Program.Repository}/releases/download/{previousVersion}/{previousArchiveName}", previousArchiveName);
                }

            var updateTestPath = Path.GetFullPath("updatetest");
            var updateFolderPath = Path.GetFullPath(Path.Combine(updateTestPath, Updater.UpdateFolderPath));
            var executablePath = Path.GetFullPath(Path.Combine(updateFolderPath, mainExecutablePath));

            if (Directory.Exists(updateTestPath))
            {
                foreach (var filename in Directory.GetFiles(updateTestPath, "*", SearchOption.AllDirectories))
                    File.SetAttributes(filename, FileAttributes.Normal);
                Directory.Delete(updateTestPath, true);
            }
            Directory.CreateDirectory(updateTestPath);

            ZipFile.ExtractToDirectory(previousArchiveName, updateTestPath);
            ZipFile.ExtractToDirectory(archiveName, updateFolderPath);

            Process.Start(new ProcessStartInfo(executablePath, $"update \"{updateTestPath}\" {previousVersion}")
            {
                WorkingDirectory = updateFolderPath,
            });
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
            if (ignoredPaths.Contains(entryName))
            {
                Trace.WriteLine($"  Skipping {path}");
                return;
            }
            if (entryName != mainExecutablePath && Path.GetExtension(entryName) == ".exe")
                entryName += "_";

            Trace.WriteLine($"  Adding {path} -> {entryName}");
            var entry = archive.CreateEntryFromFile(path, entryName, CompressionLevel.Optimal);

            var pathExtension = Path.GetExtension(path);
            if (pathExtension == ".exe" || pathExtension == ".dll")
            {
                var pdbPath = Path.Combine(Path.GetDirectoryName(path), $"{Path.GetFileNameWithoutExtension(path)}.pdb");
                if (File.Exists(pdbPath))
                    addFile(archive, pdbPath, sourceDirectory);
            }
        }
    }
}