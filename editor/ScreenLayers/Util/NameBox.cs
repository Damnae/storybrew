using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;

namespace StorybrewEditor.ScreenLayers.Util
{
    public class NameBox : UiScreenLayer
    {
        private string initialName;
        private Action<string> nameAction;

        private LinearLayout mainLayout;
        private Textbox nameTextbox;
        private LinearLayout buttonsLayout;
        private Button okButton;
        private Button cancelButton;

        public override bool IsPopup => true;

        public NameBox(string initialName, Action<string> nameAction)
        {
            this.initialName = initialName;
            this.nameAction = nameAction;
        }

        public override void Load()
        {
            base.Load();

            WidgetManager.Root.Add(mainLayout = new LinearLayout(WidgetManager)
            {
                StyleName = "panel",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.Centre,
                AnchorTo = UiAlignment.Centre,
                Padding = new FourSide(16),
                Children = new Widget[]
                {
                    nameTextbox = new Textbox(WidgetManager)
                    {
                        LabelText = "Name",
                        AnchorTo = UiAlignment.Centre,
                        Value = initialName,
                    },
                    buttonsLayout = new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        AnchorTo = UiAlignment.Centre,
                        Children = new Widget[]
                        {
                            okButton = new Button(WidgetManager)
                            {
                                Text = "Ok",
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

            okButton.OnClick += (sender, e) =>
            {
                Exit();
                nameAction?.Invoke(nameTextbox.Value);
            };
            cancelButton.OnClick += (sender, e) => Exit();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(400);
        }
    }
}
