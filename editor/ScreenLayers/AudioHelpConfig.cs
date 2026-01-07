using BrewLib.Audio;
using BrewLib.UserInterface;
using BrewLib.Util;
using ManagedBass;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorybrewEditor.ScreenLayers
{
    internal class AudioHelpConfig : UiScreenLayer
    {
        private LinearLayout layout;
        private List<DeviceInfo> devices;
        private LinearLayout buttonsLayout;
        private Button okButton;
        private Button cancelButton;

        public override void Load()
        {
            base.Load();
            devices = new List<DeviceInfo>();
            for (int i = 1; i <= Bass.DeviceCount; i++)
            {
                if (Bass.GetDeviceInfo(i, out DeviceInfo info))
                {
                    devices.Add(info);
                }
            }

            WidgetManager.Root.Add(layout = new LinearLayout(WidgetManager)
            {
                StyleName = "panel",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Padding = new FourSide(16),
                FitChildren = true,
                Fill = true,
                Children = new Widget[]
                {
                    buttonsLayout = new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        Fill = true,
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                        Children = new Widget[]
                        {
                            okButton = new Button(WidgetManager)
                            {
                                Text = "Ok",
                                AnchorFrom = BoxAlignment.Centre,
                            }
                        },
                    },
                }
            });

            okButton.OnClick += (sender, e) =>
            {
                Exit();
            };
        }
    }
}
