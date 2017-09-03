using BrewLib.UserInterface;
using BrewLib.Util;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Padding = new FourSide(16),
                FitChildren = true,
                Children = new Widget[]
                {
                    actionLabel = new Label(WidgetManager)
                    {
                        Text = "Updating",
                        AnchorFrom = BoxAlignment.Centre,
                    },
                    statusLabel = new Label(WidgetManager)
                    {
                        StyleName = "small",
                        Text = downloadUrl,
                        AnchorFrom = BoxAlignment.Centre,
                    },
                    progressBar = new ProgressBar(WidgetManager)
                    {
                        Value = 0,
                        AnchorFrom = BoxAlignment.Centre,
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
                        Trace.WriteLine($"Failed to download the new version.\n\n{exception}");
                        Manager.ShowMessage($"Failed to download the new version, please update manually.\n\n{exception}", () => Updater.OpenLastestReleasePage());
                        Exit();
                        return;
                    }
                    try
                    {
                        string executablePath = null;
                        using (var zip = ZipFile.OpenRead(Updater.UpdateArchivePath))
                        {
                            if (Directory.Exists(Updater.UpdateFolderPath))
                                Directory.Delete(Updater.UpdateFolderPath, true);

                            foreach (var entry in zip.Entries)
                            {
                                // Folders don't have a name
                                if (entry.Name.Length == 0) continue;

                                var entryPath = Path.GetFullPath(Path.Combine(Updater.UpdateFolderPath, entry.FullName));
                                var entryFolder = Path.GetDirectoryName(entryPath);

                                if (!Directory.Exists(entryFolder))
                                {
                                    Trace.WriteLine($"Creating {entryFolder}");
                                    Directory.CreateDirectory(entryFolder);
                                }

                                Trace.WriteLine($"Extracting {entryPath}");
                                entry.ExtractToFile(entryPath);

                                if (Path.GetExtension(entryPath) == ".exe")
                                    executablePath = entryPath;
                            }
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
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Failed to start the update process.\n\n{e}");
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
