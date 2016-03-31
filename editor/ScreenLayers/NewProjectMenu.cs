using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System.IO;

namespace StorybrewEditor.ScreenLayers
{
    public class NewProjectMenu : UiScreenLayer
    {
        private LinearLayout mainLayout;
        private PathSelector projectPathSelector;
        private PathSelector mapsetPathSelector;
        private Button startButton;
        private Button cancelButton;

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
                    new Label(WidgetManager)
                    {
                        Text = "New Project",
                        AnchorTo = UiAlignment.Centre,
                    },
                    mapsetPathSelector = new PathSelector(WidgetManager, PathSelectorMode.Folder)
                    {
                        Value = OsuHelper.GetOsuSongFolder(),
                        LabelText = "Mapset Path",
                        AnchorTo = UiAlignment.Centre,
                    },
                    projectPathSelector = new PathSelector(WidgetManager, PathSelectorMode.SaveFile)
                    {
                        LabelText = "Project Path",
                        AnchorTo = UiAlignment.Centre,
                        Filter = Project.FileFilter,
                        SaveExtension = Project.Extension,
                    },
                    new Label(WidgetManager)
                    {
                        StyleName = "hint",
                        Text = "Leave the Project Path empty to store the project inside the mapset's folder.",
                        AnchorTo = UiAlignment.Centre,
                    },
                    new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        AnchorTo = UiAlignment.Centre,
                        Fill = true,
                        Children = new Widget[]
                        {
                            startButton = new Button(WidgetManager)
                            {
                                Text = "Start",
                                AnchorTo = UiAlignment.Centre,
                            },
                            cancelButton = new Button(WidgetManager)
                            {
                                Text = "Cancel",
                                AnchorTo = UiAlignment.Centre,
                            },
                        },
                    },
                },
            });

            projectPathSelector.OnValueChanged += (sender, e) => updateButtonsState();
            mapsetPathSelector.OnValueChanged += (sender, e) => updateButtonsState();
            updateButtonsState();

            startButton.OnClick += (sender, e) => createProject();
            cancelButton.OnClick += (sender, e) => Exit();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(300, 0);
        }

        private void createProject()
        {
            var projectFilename = projectPathSelector.Value;
            if (string.IsNullOrWhiteSpace(projectFilename))
                projectFilename = Path.Combine(mapsetPathSelector.Value, Project.DefaultFilename);

            Manager.AsyncLoading("Creating project...", () =>
            {
                var project = new Project(projectFilename)
                {
                    MapsetPath = mapsetPathSelector.Value,
                };
                project.Save();
                Program.Schedule(() => Manager.Set(new ProjectMenu(project)));
            });
        }

        private void updateButtonsState()
        {
            startButton.Disabled = !areFieldsValid();
        }

        private bool areFieldsValid()
        {
            if (!Directory.Exists(mapsetPathSelector.Value))
                return false;

            var autoProjectPath = string.IsNullOrWhiteSpace(projectPathSelector.Value);
            if (autoProjectPath && File.Exists(Path.Combine(mapsetPathSelector.Value, Project.DefaultFilename)))
                return false;

            if (!autoProjectPath && !Directory.Exists(Path.GetDirectoryName(projectPathSelector.Value)))
                return false;

            if (Directory.GetFiles(mapsetPathSelector.Value, "*.osu", SearchOption.TopDirectoryOnly).Length == 0)
                return false;

            return true;
        }
    }
}
