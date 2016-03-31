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
        private Label updateDescriptionLabel;
        private Button updateButton;

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
                        StyleName = "small",
                        AnchorTo = UiAlignment.Centre,
                        Displayed = false,
                    },
                    updateDescriptionLabel = new Label(WidgetManager)
                    {
                        StyleName = "hint",
                        AnchorTo = UiAlignment.Centre,
                        Displayed = false,
                    },
                    new Label(WidgetManager)
                    {
                        StyleName = "hint",
                        Text = Program.FullName,
                        AnchorTo = UiAlignment.Centre,
                    },
                },
            });

            newProjectButton.OnClick += (sender, e) => Manager.Add(new NewProjectMenu());
            openProjectButton.OnClick += (sender, e) =>
            {
                Manager.OpenFilePicker("", "", Project.FileFilter, (path) =>
                {
                    Manager.AsyncLoading("Loading project...", () =>
                    {
                        var project = Project.Load(path);
                        Program.Schedule(() => Manager.Set(new ProjectMenu(project)));
                    });
                });
            };
            closeButton.OnClick += (sender, e) => Exit();
            updateButton.OnClick += (sender, e) => Process.Start($"https://github.com/{Program.Repository}/releases/latest");

            checkLatestVersion();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(300);
            bottomLayout.Pack(600);
        }

        private void checkLatestVersion()
        {
            NetHelper.Request($"https://api.github.com/repos/{Program.Repository}/releases/latest", "cache/net/latestrelease", 15 * 60, (response, exception) =>
            {
                Program.Schedule(() =>
                {
                    if (IsDisposed)
                        return;
                    if (exception != null)
                    {
                        handleLastestVersionException(exception);
                        return;
                    }
                    try
                    {
                        var jsonResponse = JObject.Parse(response);

                        var name = jsonResponse.Value<string>("name");
                        var body = jsonResponse.Value<string>("body");
                        var publishedAt = jsonResponse.Value<string>("published_at");
                        var authorName = jsonResponse.GetValue("author").Value<string>("login");

                        var latestVersion = new Version(name);
                        if (Program.Version < latestVersion)
                        {
                            var date = DateTime.ParseExact(publishedAt, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            if (body.Contains("---")) body = body.Substring(0, body.IndexOf("---"));
                            body += $"\n\nPublished {date.ToTimeAgo()} ({date.ToShortDateString()}) by {authorName}.";

                            updateButton.Text = $"Version {latestVersion} available!";
                            updateDescriptionLabel.Text = body;
                            updateButton.Displayed = updateDescriptionLabel.Displayed = true;
                            bottomLayout.Pack(600);
                        }
                    }
                    catch (Exception e)
                    {
                        handleLastestVersionException(e);
                    }
                });
            });
        }

        private void handleLastestVersionException(Exception exception)
        {
            Trace.WriteLine($"Error while retrieving latest release information: {exception.Message}");

            updateButton.Text = "See latest release";
            updateDescriptionLabel.Text = $"Could not retrieve latest release information:\n{exception.Message}";
            updateButton.Displayed = updateDescriptionLabel.Displayed = true;
            bottomLayout.Pack(600);
        }
    }
}
