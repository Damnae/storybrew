using OpenTK;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.Util;
using System;

namespace StorybrewEditor.UserInterface.Components
{
    public class LayerList : Widget
    {
        private LinearLayout layout;
        private LinearLayout layersLayout;
        private Project project;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public LayerList(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;

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
                        Text = "Layers",
                        CanGrow = false,
                    },
                    new ScrollArea(manager, layersLayout = new LinearLayout(manager)
                    {
                        FitChildren = true,
                    }),
                },
            });

            project.OnLayersChanged += project_OnLayersChanged;
            refreshLayers();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                project.OnLayersChanged -= project_OnLayersChanged;
            }
            project = null;
            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }

        private void project_OnLayersChanged(object sender, EventArgs e)
            => refreshLayers();

        private void refreshLayers()
        {
            layersLayout.ClearWidgets();
            var index = 0;
            foreach (var layer in project.Layers)
            {
                var effect = layer.Effect;

                Widget layerRoot;
                Label nameLabel, effectNameLabel;
                Button showHideButton, moveUpButton, moveDownButton, moveTopButton, moveBottomButton;
                layersLayout.Add(layerRoot = new LinearLayout(Manager)
                {
                    AnchorFrom = UiAlignment.Centre,
                    AnchorTo = UiAlignment.Centre,
                    Horizontal = true,
                    FitChildren = true,
                    Fill = true,
                    Children = new Widget[]
                    {
                        new LinearLayout(Manager)
                        {
                            StyleName = "condensed",
                            Children = new Widget[]
                            {
                                nameLabel = new Label(Manager)
                                {
                                    StyleName = "listItem",
                                    Text = layer.Name,
                                    AnchorFrom = UiAlignment.Left,
                                    AnchorTo = UiAlignment.Left,
                                },
                                effectNameLabel = new Label(Manager)
                                {
                                    StyleName = "listItemSecondary",
                                    Text = $"using {effect.BaseName}",
                                    AnchorFrom = UiAlignment.Left,
                                    AnchorTo = UiAlignment.Left,
                                },
                            },
                        },
                        moveTopButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.AngleDoubleUp,
                            Tooltip = "Top",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
                            Disabled = index == 0,
                        },
                        moveUpButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.AngleUp,
                            Tooltip = "Up",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
                            Disabled = index == 0,
                        },
                        moveDownButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.AngleDown,
                            Tooltip = "Down",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
                            Disabled = index == project.LayersCount - 1,
                        },
                        moveBottomButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.AngleDoubleDown,
                            Tooltip = "Bottom",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
                            Disabled = index == project.LayersCount - 1,
                        },
                        showHideButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = layer.Visible ? IconFont.Eye : IconFont.EyeSlash,
                            Tooltip = "Show/Hide",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            Checkable = true,
                            Checked = layer.Visible,
                            CanGrow = false,
                        },
                    },
                });

                var la = layer;

                EventHandler changedHandler, effectChangedHandler;
                layer.OnChanged += changedHandler = (sender, e) =>
                {
                    nameLabel.Text = la.Name;
                    showHideButton.Icon = layer.Visible ? IconFont.Eye : IconFont.EyeSlash;
                    showHideButton.Checked = la.Visible;
                };
                effect.OnChanged += effectChangedHandler = (sender, e) =>
                {
                    effectNameLabel.Text = $"using {effect.BaseName}";
                };
                layerRoot.OnDisposed += (sender, e) =>
                {
                    la.OnChanged -= changedHandler;
                    effect.OnChanged -= effectChangedHandler;
                };

                moveUpButton.OnClick += (sender, e) => project.MoveUp(la);
                moveDownButton.OnClick += (sender, e) => project.MoveDown(la);
                moveTopButton.OnClick += (sender, e) => project.MoveTop(la);
                moveBottomButton.OnClick += (sender, e) => project.MoveBottom(la);
                showHideButton.OnValueChanged += (sender, e) => la.Visible = showHideButton.Checked;
                index++;
            }
        }
    }
}
