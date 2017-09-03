using BrewLib.UserInterface;
using BrewLib.Util;
using System;

namespace StorybrewEditor.ScreenLayers.Util
{
    public class MessageBox : UiScreenLayer
    {
        private string message;
        private LinearLayout mainLayout;
        private LinearLayout buttonsLayout;

        private Action yesAction;
        private Action noAction;
        private bool cancelable;

        public override bool IsPopup => true;

        public MessageBox(string message, Action okAction = null) : this(message, okAction, null, false) { }
        public MessageBox(string message, Action okAction, bool cancelable) : this(message, okAction, null, cancelable) { }
        public MessageBox(string message, Action yesAction, Action noAction, bool cancelable)
        {
            this.message = message;
            this.yesAction = yesAction;
            this.noAction = noAction;
            this.cancelable = cancelable;
        }

        public override void Load()
        {
            base.Load();

            WidgetManager.Root.Add(mainLayout = new LinearLayout(WidgetManager)
            {
                StyleName = "panel",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Padding = new FourSide(16),
                Children = new Widget[]
                {
                    new ScrollArea(WidgetManager, new Label(WidgetManager)
                    {
                        Text = message,
                        AnchorFrom = BoxAlignment.Centre,
                    })
                    {
                        ScrollsHorizontally = true,
                    },
                    buttonsLayout = new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        AnchorFrom = BoxAlignment.Centre,
                    },
                },
            });

            var yesButton = new Button(WidgetManager)
            {
                Text = noAction != null ? "Yes" : "Ok",
                AnchorFrom = BoxAlignment.Centre,
            };
            yesButton.OnClick += (sender, e) =>
            {
                Exit();
                yesAction?.Invoke();
            };
            buttonsLayout.Add(yesButton);

            if (noAction != null)
            {
                var noButton = new Button(WidgetManager)
                {
                    Text = "No",
                    AnchorFrom = BoxAlignment.Centre,
                };
                noButton.OnClick += (sender, e) =>
                {
                    Exit();
                    noAction.Invoke();
                };
                buttonsLayout.Add(noButton);
            }

            if (cancelable)
            {
                var cancelButton = new Button(WidgetManager)
                {
                    Text = "Cancel",
                    AnchorFrom = BoxAlignment.Centre,
                };
                cancelButton.OnClick += (sender, e) => Exit();
                buttonsLayout.Add(cancelButton);
            }
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(400, 0, 1024 - 32, 768 - 32);
        }
    }
}
