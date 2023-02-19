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
        readonly LinearLayout layout;
        Project project;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public SettingsMenu(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;

            Button referencedAssemblyButton, floatingPointTimeButton, helpButton, displayWarningbutton;
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
                        CanGrow = false
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
                                AnchorTo = BoxAlignment.Centre
                            },
                            referencedAssemblyButton = new Button(manager)
                            {
                                Text = "View referenced assemblies",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre
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
                                        Text = "Dim"
                                    },
                                    dimSlider = new Slider(manager)
                                    {
                                        StyleName = "small",
                                        AnchorFrom = BoxAlignment.Centre,
                                        AnchorTo = BoxAlignment.Centre,
                                        Value = 0,
                                        Step = .05f
                                    }
                                }
                            },
                            floatingPointTimeButton = new Button(manager)
                            {
                                Text = "Export time as floating-point",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                                Checkable = true,
                                Checked = project.ExportSettings.UseFloatForTime,
                                Tooltip = "A storyboard exported with this option enabled\nwill only be compatible with lazer."
                            },
                            displayWarningbutton = new Button(manager)
                            {
                                Text = "Toggle debug warnings",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                                Checkable = true,
                                Checked = project.DisplayDebugWarning,
                                Tooltip = "Toggle to display debug diagnostics about\nyour storyboard."
                            }
                        }
                    }
                }
            });

            helpButton.OnClick += (sender, e) => Process.Start($"https://github.com/{Program.Repository}/wiki");
            referencedAssemblyButton.OnClick += (sender, e) => Manager.ScreenLayerManager.Add(new ReferencedAssemblyConfig(project));
            dimSlider.OnValueChanged += (sender, e) =>
            {
                project.DimFactor = dimSlider.Value;
                dimLabel.Text = $"Dim ({project.DimFactor:p})";
            };
            floatingPointTimeButton.OnValueChanged += (sender, e) => project.ExportSettings.UseFloatForTime = floatingPointTimeButton.Checked;
            displayWarningbutton.OnValueChanged += (sender, e) => project.DisplayDebugWarning = displayWarningbutton.Checked;
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