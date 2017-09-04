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
        private LinearLayout layout;
        private Project project;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public SettingsMenu(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;

            Button referencedAssemblyButton, helpButton;

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
                            referencedAssemblyButton = new Button(manager)
                            {
                                Text = "Referenced Assemblies",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                            },
                            helpButton = new Button(manager)
                            {
                                Text = "Help!",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                            },
                        }
                    }
                },
            });

            helpButton.OnClick += (sender, e) => Process.Start($"https://github.com/{Program.Repository}/wiki");
            referencedAssemblyButton.OnClick += (sender, e) => Manager.ScreenLayerManager.Add(new ReferencedAssemblyConfig(project));
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
