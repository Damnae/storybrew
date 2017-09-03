using BrewLib.UserInterface;
using BrewLib.Util;
using System;

namespace StorybrewEditor.ScreenLayers.Util
{
    public class PromptBox : UiScreenLayer
    {
        private string title;
        private string description;
        private string initialText;
        private Action<string> action;

        private LinearLayout mainLayout;
        private Textbox textbox;
        private LinearLayout buttonsLayout;
        private Button okButton;
        private Button cancelButton;

        public override bool IsPopup => true;

        public PromptBox(string title, string description, string initialText, Action<string> action)
        {
            this.title = title;
            this.description = description;
            this.initialText = initialText;
            this.action = action;
        }

        public override void Load()
        {
            base.Load();

            Label descriptionLabel;
            WidgetManager.Root.Add(mainLayout = new LinearLayout(WidgetManager)
            {
                StyleName = "panel",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Padding = new FourSide(16),
                Children = new Widget[]
                {
                    descriptionLabel = new Label(WidgetManager)
                    {
                        StyleName = "small",
                        Text = description,
                        AnchorFrom = BoxAlignment.Centre,
                    },
                    textbox = new Textbox(WidgetManager)
                    {
                        LabelText = title,
                        AnchorFrom = BoxAlignment.Centre,
                        Value = initialText,
                    },
                    buttonsLayout = new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        AnchorFrom = BoxAlignment.Centre,
                        Children = new Widget[]
                        {
                            okButton = new Button(WidgetManager)
                            {
                                Text = "Ok",
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

            if (string.IsNullOrWhiteSpace(description))
                descriptionLabel.Dispose();

            okButton.OnClick += (sender, e) =>
            {
                Exit();
                action?.Invoke(textbox.Value);
            };
            cancelButton.OnClick += (sender, e) => Exit();
        }

        public override void OnTransitionIn()
        {
            base.OnTransitionIn();

            WidgetManager.KeyboardFocus = textbox;
            textbox.SelectAll();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(400);
        }
    }
}
