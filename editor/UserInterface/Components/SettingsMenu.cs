using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using System;
using System.Collections.Generic;
using StorybrewEditor.ScreenLayers;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorybrewEditor.UserInterface.Components
{
    class SettingsMenu : Widget
    {
        private LinearLayout layout;
        private ReferencedAssemblyUi referencedAssemblyUi;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public SettingsMenu(WidgetManager manager, ReferencedAssemblyUi referencedAssemblyUi) : base(manager)
        {
            this.referencedAssemblyUi = referencedAssemblyUi;

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
            referencedAssemblyButton.OnClick += (sender, e) =>
            {
                // NOTE: We may need to keep in mind about the effect config UI.
                // If it's not necessary, then we can just use a toggle statement here.

                // TODO: This item will be on top of the effect config UI. The effect config UI may need
                // to be tightly coupled with the referenced assembly UI (bad), or there needs to be some
                // other consideration around all of this.
                if (!referencedAssemblyUi.Displayed)
                {
                    referencedAssemblyUi.Displayed = true;
                }
                else referencedAssemblyUi.Displayed = false;
            };
        }

        protected override void Dispose(bool disposing)
        {
            // NOTE: May not be necessary unless we need the other managers
            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
}
