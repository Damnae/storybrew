using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.Storyboarding;
using System.Diagnostics;

namespace StorybrewEditor.UserInterface.Components
{
    public class SettingsMenu : Widget
    {
        private readonly LinearLayout layout;
        private Project project;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public SettingsMenu(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;

            Button referencedAssemblyButton, helpButton;
            Label dimLabel;
            Slider dimSlider;

            Add(layout = new LinearLayout(manager)
            {
                StyleName = "panel",
                Padding = new FourSide(16),
                FitChildren = true,
                Fill = true,
                Children = new Widget[]
                    {
                    new Label(manager)
                    {
                        Text = "Settings",
                        CanGrow = false,
                    },
                    new LinearLayout(manager)
                    {
                        Fill = true,
                        FitChildren = true,
                        CanGrow = false,
                        Children = new Widget[]
                        {
                            helpButton = new Button(manager)
                            {
                                Text = "Help!",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                            },
                            referencedAssemblyButton = new Button(manager)
                            {
                                Text = "Referenced Assemblies",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                            },
                            new LinearLayout(manager)
                            {
                                StyleName = "condensed",
                                FitChildren = true,
                                Children = new Widget[]
                                {
                                    dimLabel = new Label(manager)
                                    {
                                        StyleName = "small",
                                        Text = "Dim",
                                    },
                                    dimSlider = new Slider(manager)
                                    {
                                        StyleName = "small",
                                        AnchorFrom = BoxAlignment.Centre,
                                        AnchorTo = BoxAlignment.Centre,
                                        Value = 0,
                                        Step = .05f,
                                    },
                                }
                            }
                        }
                    }
                },
            });

            helpButton.OnClick += (sender, e) => Process.Start($"https://github.com/{Program.Repository}/wiki");
            referencedAssemblyButton.OnClick += (sender, e) => Manager.ScreenLayerManager.Add(new ReferencedAssemblyConfig(project));
            dimSlider.OnValueChanged += (sender, e) =>
            {
                project.DimFactor = dimSlider.Value;
                dimLabel.Text = $"Dim ({project.DimFactor:p})";
            };
        }

        protected override void Dispose(bool disposing)
        {
            project = null;
            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
}
