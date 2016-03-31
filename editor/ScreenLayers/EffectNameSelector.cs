using System;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;

namespace StorybrewEditor.ScreenLayers
{
    public class EffectNameSelector : UiScreenLayer
    {
        private Project project;
        private Action<string> callback;

        private LinearLayout mainLayout;
        private Button cancelButton;

        public override bool IsPopup => true;

        public EffectNameSelector(Project project, Action<string> callback)
        {
            this.project = project;
            this.callback = callback;
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
                                Text = "Select an effect",
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

            foreach (var effectName in project.GetEffectNames())
            {
                Button button;
                mainLayout.Add(button = new Button(WidgetManager)
                {
                    StyleName = "small",
                    Text = effectName,
                    AnchorTo = UiAlignment.Centre,
                });

                var result = effectName;
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
    }
}
