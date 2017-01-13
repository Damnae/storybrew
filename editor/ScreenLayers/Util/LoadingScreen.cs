using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using System;
using System.Diagnostics;
using System.Threading;

namespace StorybrewEditor.ScreenLayers.Util
{
    public class LoadingScreen : UiScreenLayer
    {
        private string title;
        private Action action;

        private LinearLayout mainLayout;

        public override bool IsPopup => true;

        public LoadingScreen(string title, Action action)
        {
            this.title = title;
            this.action = action;
        }

        public override void Load()
        {
            var thread = new Thread(() =>
            {
                Exception exception = null;
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    exception = e;
                }
                Program.Schedule(() =>
                {
                    if (exception != null)
                    {
                        Trace.WriteLine($"{title} failed ({action.Method.Name}): {exception}");

                        var exceptionMessage = $"{exception.Message} ({exception.GetType().Name})";
                        var innerException = exception.InnerException;
                        while (innerException != null)
                        {
                            exceptionMessage += $"\nCaused by: {innerException.Message} ({innerException.GetType().Name})";
                            innerException = innerException.InnerException;
                        }
                        Manager.ShowMessage($"{title} failed:\n\n{exceptionMessage}\n\nDetails:\n{exception.GetBaseException()}");
                    }
                    Exit();
                });
            })
            { Name = $"Loading ({title}, {action.Method.Name})", IsBackground = true, };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            base.Load();
            WidgetManager.Root.Add(mainLayout = new LinearLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Bottom,
                AnchorTo = BoxAlignment.Bottom,
                Offset = new Vector2(0, -64),
                Padding = new FourSide(16),
                FitChildren = true,
                Horizontal = true,
                Children = new Widget[]
                {
                    new Label(WidgetManager)
                    {
                        Text = $"{title}..." ?? "Loading...",
                    }
                },
            });
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(1024);
        }

        public override void Close()
        {
        }
    }
}
