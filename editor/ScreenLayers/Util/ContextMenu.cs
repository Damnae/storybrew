using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;

namespace StorybrewEditor.ScreenLayers
{
    public class ContextMenu<T> : UiScreenLayer
    {
        private string title;
        private Action<T> callback;
        private List<Option> options = new List<Option>();

        private LinearLayout mainLayout;
        private Button cancelButton;

        public override bool IsPopup => true;

        public ContextMenu(string title, Action<T> callback, params Option[] options)
        {
            this.title = title;
            this.callback = callback;
            this.options.AddRange(options);
        }

        public ContextMenu(string title, Action<T> callback, params T[] options)
        {
            this.title = title;
            this.callback = callback;
            foreach (var option in options)
                this.options.Add(new Option(option.ToString(), option));
        }

        public ContextMenu(string title, Action<T> callback, IEnumerable<T> options)
        {
            this.title = title;
            this.callback = callback;
            foreach (var option in options)
                this.options.Add(new Option(option.ToString(), option));
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
                FitChildren = true,
                Children = new Widget[]
                {
                    new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(WidgetManager)
                            {
                                Text = title,
                            },
                            cancelButton = new Button(WidgetManager)
                            {
                                StyleName = "icon",
                                Icon = IconFont.TimesCircle,
                                AnchorTo = UiAlignment.Centre,
                                CanGrow = false,
                            }
                        },
                    },
                },
            });
            cancelButton.OnClick += (sender, e) => Exit();

            foreach (var option in options)
            {
                Button button;
                mainLayout.Add(button = new Button(WidgetManager)
                {
                    StyleName = "small",
                    Text = option.Name,
                    AnchorTo = UiAlignment.Centre,
                });

                var result = option.Value;
                button.OnClick += (sender, e) =>
                {
                    callback.Invoke(result);
                    Exit();
                };
            }
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            mainLayout.Pack(400, 0);
        }

        public struct Option
        {
            public readonly string Name;
            public readonly T Value;

            public Option(string name, T value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}
