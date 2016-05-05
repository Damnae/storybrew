using Librairies;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace StorybrewEditor.ScreenLayers
{
    public class UpdateMenu : UiScreenLayer
    {
        private string downloadUrl;

        private LinearLayout mainLayout;
        private Label actionLabel;
        private Label statusLabel;
        private ProgressBar progressBar;

        public UpdateMenu(string downloadUrl)
        {
            this.downloadUrl = downloadUrl;
        }

        public override void Load()
        {
            base.Load();

            WidgetManager.Root.Add(mainLayout = new LinearLayout(WidgetManager)
            {
                StyleName = "panel",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.Centre,
                AnchorTo = UiAlignment.Centre,
                Padding = new FourSide(16),
                FitChildren = true,
                Children = new Widget[]
                {
                    actionLabel = new Label(WidgetManager)
                    {
                        Text = "Updating",
                        AnchorTo = UiAlignment.Centre,
                    },
                    statusLabel = new Label(WidgetManager)
                    {
                        StyleName = "hint",
                        Text = downloadUrl,
                        AnchorTo = UiAlignment.Centre,
                    },
                    progressBar = new ProgressBar(WidgetManager)
                    {
                        Value = 0,
                        AnchorTo = UiAlignment.Centre,
                    },
                },
            });

            NetHelper.Download(downloadUrl, Updater.UpdateArchivePath,
                (progress) =>
                {
                    if (IsDisposed) return false;
                    progressBar.Value = progress;
                    return true;
                },
                (exception) =>
                {
                    if (IsDisposed) return;
                    if (exception != null)
                    {
                        Manager.ShowMessage($"Failed to download the new version, please update manually.\n\n{exception}", () => Updater.OpenLastestReleasePage());
                        Exit();
                        return;
                    }
                    try
                    {
                        using (var zip = ZipStorer.Open(Updater.UpdateArchivePath, FileAccess.Read))
                        {
                            if (Directory.Exists(Updater.UpdateFolderPath))
                                Directory.Delete(Updater.UpdateFolderPath, true);

                            string executablePath = null;
                            var entries = zip.ReadCentralDir();
                            foreach (var entry in entries)
                            {
                                var entryPath = Path.GetFullPath(Path.Combine(Updater.UpdateFolderPath, entry.FilenameInZip));
                                Debug.Print($"Extracting {entryPath}");
                                zip.ExtractFile(entry, entryPath);

                                if (Path.GetExtension(entryPath) == ".exe")
                                    executablePath = entryPath;
                            }

                            actionLabel.Text = "Updating";

                            var localPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                            var process = new Process()
                            {
                                StartInfo = new ProcessStartInfo(executablePath, $"update \"{localPath}\" {Program.Version}")
                                {
                                    WorkingDirectory = Updater.UpdateFolderPath,
                                },
                            };
                            if (process.Start())
                                Manager.Exit();
                            else
                            {
                                Manager.ShowMessage("Failed to start the update process, please update manually.", () => Updater.OpenLastestReleasePage());
                                Exit();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Manager.ShowMessage($"Failed to start the update process, please update manually.\n\n{e}", () => Updater.OpenLastestReleasePage());
                        Exit();
                    }
                });
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(300, 0);
        }
    }
}
