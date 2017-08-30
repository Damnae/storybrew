using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorybrewEditor.UserInterface.Components
{
    public class ReferencedAssemblyUi : Widget
    {
        private LinearLayout layout;
        private LinearLayout assembliesLayout;
        private Project project;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public ReferencedAssemblyUi(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;

            Button addAssemblyButton, closeButton, debugButton;
            Add(layout = new LinearLayout(manager)
            {
                StyleName = "panel",
                Padding = new FourSide(16),
                FitChildren = true,
                Fill = true,
                Children = new Widget[]
                {
                    new LinearLayout(manager)
                    {
                        Fill = true,
                        FitChildren = true,
                        Horizontal = true,
                        CanGrow = false,
                        Children = new Widget[]
                        {
                            new Label(manager)
                            {
                                Text = "Referenced Assemblies",
                            },
                            closeButton = new Button(Manager)
                            {
                                StyleName = "icon",
                                Icon = IconFont.TimesCircle,
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                                CanGrow = false,
                            },
                        },
                    },
                    new ScrollArea(manager, assembliesLayout = new LinearLayout(manager)
                    {
                        FitChildren = true,
                    }),
                   new LinearLayout(manager)
                   {
                       Fill = true,
                       FitChildren = true,
                       CanGrow = false,
                       Children = new Widget[]
                       {
                        addAssemblyButton = new Button(manager)
                        {
                            StyleName = "small",
                            Text = "Add assembly file",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                        },
                        debugButton = new Button(manager)
                        {
                            Text = "(^_-)-☆",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                        },
                       },
                   },
                }
            });

            closeButton.OnClick += (sender, e) => Displayed = false;

            // Prompt to find DLL
            // And bam, it's added to the list.
            // TODO: Validation.
            addAssemblyButton.OnClick += (sender, e) => Manager.ScreenLayerManager.OpenFilePicker("", "", Project.ProjectsFolder, ".dll files (*.dll)|*.dll", (path) => project.AddReferencedAssembly(path));

            debugButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowMessage(project.GetReferencedAssemblies());
        }

        protected override void Dispose(bool disposing)
        {
            // NOTE: May not be necessary, only if we need project
            // We might this time lol
            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }

        
    }
}
