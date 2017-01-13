using BrewLib.UserInterface;
using BrewLib.Util;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System.IO;

namespace StorybrewEditor.ScreenLayers
{
    public class NewProjectMenu : UiScreenLayer
    {
        private LinearLayout mainLayout;
        private Textbox projectNameTextbox;
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
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Padding = new FourSide(16),
                FitChildren = true,
                Children = new Widget[]
                {
                    new Label(WidgetManager)
                    {
                        Text = "New Project",
                        AnchorFrom = BoxAlignment.Centre,
                    },
                    projectNameTextbox = new Textbox(WidgetManager)
                    {
                        LabelText = "Project Name",
                        AnchorFrom = BoxAlignment.Centre,
                    },
                    mapsetPathSelector = new PathSelector(WidgetManager, PathSelectorMode.OpenDirectory)
                    {
                        Value = OsuHelper.GetOsuSongFolder(),
                        LabelText = "Mapset Path",
                        AnchorFrom = BoxAlignment.Centre,
                        Filter = ".osu files (*.osu)|*.osu",
                    },
                    new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        AnchorFrom = BoxAlignment.Centre,
                        Fill = true,
                        Children = new Widget[]
                        {
                            startButton = new Button(WidgetManager)
                            {
                                Text = "Start",
                                AnchorFrom = BoxAlignment.Centre,
                            },
                            cancelButton = new Button(WidgetManager)
                            {
                                Text = "Cancel",
                                AnchorFrom = BoxAlignment.Centre,
                            },
                        },
                    },
                },
            });

            projectNameTextbox.OnValueChanged += (sender, e) => updateButtonsState();
            projectNameTextbox.OnValueCommited += (sender, e) =>
            {
                var name = projectNameTextbox.Value;
                foreach (var character in Path.GetInvalidFileNameChars())
                    name = name.Replace(character, '_');
                projectNameTextbox.Value = name;
            };
            mapsetPathSelector.OnValueChanged += (sender, e) => updateButtonsState();
            mapsetPathSelector.OnValueCommited += (sender, e) =>
            {
                if (!Directory.Exists(mapsetPathSelector.Value) && File.Exists(mapsetPathSelector.Value))
                {
                    mapsetPathSelector.Value = Path.GetDirectoryName(mapsetPathSelector.Value);
                    return;
                }
                updateButtonsState();
            };
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
            Manager.AsyncLoading("Creating project", () =>
            {
                var project = Project.Create(projectNameTextbox.Value, mapsetPathSelector.Value, true);
                Program.Schedule(() => Manager.Set(new ProjectMenu(project)));
            });
        }

        private void updateButtonsState()
        {
            startButton.Disabled = !updateFieldsValid();
        }

        private bool updateFieldsValid()
        {
            var projectFolderName = projectNameTextbox.Value;
            if (string.IsNullOrWhiteSpace(projectFolderName))
            {
                startButton.Tooltip = $"The project name isn't valid";
                return false;
            }

            var projectFolderPath = Path.Combine(Project.ProjectsFolder, projectFolderName);
            if (Directory.Exists(projectFolderPath))
            {
                startButton.Tooltip = $"A project named '{projectFolderName}' already exists";
                return false;
            }

            if (!Directory.Exists(mapsetPathSelector.Value))
            {
                startButton.Tooltip = "The selected mapset folder does not exist";
                return false;
            }

            if (Directory.GetFiles(mapsetPathSelector.Value, "*.osu", SearchOption.TopDirectoryOnly).Length == 0)
            {
                startButton.Tooltip = $"No .osu found in the selected mapset folder";
                return false;
            }

            startButton.Tooltip = string.Empty;
            return true;
        }
    }
}
