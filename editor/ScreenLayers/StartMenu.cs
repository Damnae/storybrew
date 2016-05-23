using Newtonsoft.Json.Linq;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.Globalization;

namespace StorybrewEditor.ScreenLayers
{
    public class StartMenu : UiScreenLayer
    {
        private LinearLayout mainLayout;
        private Button newProjectButton;
        private Button openProjectButton;
        private Button preferencesButton;
        private Button closeButton;

        private LinearLayout bottomLayout;
        private Button updateButton;
        private Label versionLabel;

        public override void Load()
        {
            base.Load();

            WidgetManager.Root.StyleName = "panel";
            WidgetManager.Root.Add(mainLayout = new LinearLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.Centre,
                AnchorTo = UiAlignment.Centre,
                Padding = new FourSide(16),
                FitChildren = true,
                Children = new Widget[]
                {
                    newProjectButton = new Button(WidgetManager)
                    {
                        Text = "New project",
                        AnchorTo = UiAlignment.Centre,
                    },
                    openProjectButton = new Button(WidgetManager)
                    {
                        Text = "Open project",
                        AnchorTo = UiAlignment.Centre,
                    },
                    preferencesButton = new Button(WidgetManager)
                    {
                        Text = "Preferences",
                        AnchorTo = UiAlignment.Centre,
                        Disabled = true,
                    },
                    closeButton = new Button(WidgetManager)
                    {
                        Text = "Close",
                        AnchorTo = UiAlignment.Centre,
                    },
                },
            });

            WidgetManager.Root.Add(bottomLayout = new LinearLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.Bottom,
                AnchorTo = UiAlignment.Bottom,
                Padding = new FourSide(16),
                Children = new Widget[]
                {
                    updateButton = new Button(WidgetManager)
                    {
                        AnchorTo = UiAlignment.Centre,
                        Displayed = false,
                    },
                    versionLabel = new Label(WidgetManager)
                    {
                        StyleName = "small",
                        Text = Program.FullName,
                        AnchorTo = UiAlignment.Centre,
                    },
                },
            });

            newProjectButton.OnClick += (sender, e) => Manager.Add(new NewProjectMenu());
            openProjectButton.OnClick += (sender, e) =>
            {
                Manager.OpenFilePicker("", "", Project.ProjectsFolder, Project.FileFilter, (projectPath) =>
                {
                    if (!PathHelper.FolderContainsPath(Project.ProjectsFolder, projectPath))
                        migrateProject(projectPath);
                    else openProject(projectPath);
                });
            };
            closeButton.OnClick += (sender, e) => Exit();
            checkLatestVersion();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(300);
            bottomLayout.Pack(600);
        }

        private void openProject(string projectPath)
        {
            Manager.AsyncLoading("Loading project...", () =>
            {
                var project = Project.Load(projectPath);
                Program.Schedule(() => Manager.Set(new ProjectMenu(project)));
            });
        }

        private void migrateProject(string projectPath)
        {
            Manager.ShowPrompt("Project name", "Projects are now placed in their own folder under the 'projects' folder.\n\nThis project will be moved there, please choose a name for it.", (projectFolderName) =>
            {
                try
                {
                    var newProjectPath = Project.Migrate(projectPath, projectFolderName);
                    openProject(newProjectPath);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Project migration for {projectPath} failed:\n{e}");
                    Manager.ShowMessage($"Project migration failed:\n{e.Message}", () => migrateProject(projectPath));
                }
            });
        }

        private void checkLatestVersion()
        {
            NetHelper.Request($"https://api.github.com/repos/{Program.Repository}/releases/latest", "cache/net/latestrelease", 15 * 60,
                (response, exception) =>
                {
                    if (IsDisposed) return;
                    if (exception != null)
                    {
                        handleLastestVersionException(exception);
                        return;
                    }
                    try
                    {
                        var jsonResponse = JObject.Parse(response);

                        var name = jsonResponse.Value<string>("name");
                        var latestVersion = new Version(name);

                        var authorName = jsonResponse.GetValue("author").Value<string>("login");

                        var body = jsonResponse.Value<string>("body");
                        if (body.Contains("---")) body = body.Substring(0, body.IndexOf("---"));

                        var publishedAt = jsonResponse.Value<string>("published_at");
                        var date = DateTime.ParseExact(publishedAt, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                        if (Program.Version < latestVersion)
                        {
                            string downloadUrl = null;
                            var assets = jsonResponse.GetValue("assets");
                            foreach (var asset in assets)
                            {
                                var downloadName = asset.Value<string>("name");
                                if (downloadName.EndsWith(".zip"))
                                {
                                    downloadUrl = asset.Value<string>("browser_download_url");
                                    break;
                                }
                            }

                            updateButton.Text = $"Version {latestVersion} available!";
                            updateButton.Tooltip = $"What's new:\n\n{body}\n\nPublished {date.ToTimeAgo()} by {authorName}.";
                            updateButton.OnClick += (sender, e) =>
                            {
                                if (downloadUrl != null && latestVersion >= new Version(1, 4))
                                    Manager.Add(new UpdateMenu(downloadUrl));
                                else Updater.OpenLastestReleasePage();
                            };
                            updateButton.Displayed = true;
                            bottomLayout.Pack(600);
                        }
                    }
                    catch (Exception e)
                    {
                        handleLastestVersionException(e);
                    }
                });
        }

        private void handleLastestVersionException(Exception exception)
        {
            Trace.WriteLine($"Error while retrieving latest release information: {exception.Message}");

            versionLabel.Text = $"Could not retrieve latest release information:\n{exception.Message}\n\n{versionLabel.Text}";

            updateButton.StyleName = "small";
            updateButton.Text = "See latest release";
            updateButton.OnClick += (sender, e) => Updater.OpenLastestReleasePage();
            updateButton.Displayed = true;
            bottomLayout.Pack(600);
        }
    }
}
