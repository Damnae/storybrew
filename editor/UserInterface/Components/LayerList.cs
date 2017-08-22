using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.Storyboarding;
using System;

namespace StorybrewEditor.UserInterface.Components
{
    public class LayerList : Widget
    {
        private LinearLayout layout;
        private LinearLayout layersLayout;
        private LayerManager layerManager;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public event Action<EditorStoryboardLayer> OnLayerPreselect;
        public event Action<EditorStoryboardLayer> OnLayerSelected;

        public LayerList(WidgetManager manager, LayerManager layerManager) : base(manager)
        {
            this.layerManager = layerManager;

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

            layerManager.OnLayersChanged += layerManager_OnLayersChanged;
            refreshLayers();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                layerManager.OnLayersChanged -= layerManager_OnLayersChanged;
            }
            layerManager = null;
            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }

        private void layerManager_OnLayersChanged(object sender, EventArgs e)
            => refreshLayers();

        private void refreshLayers()
        {
            layersLayout.ClearWidgets();
            foreach (var osbLayer in Project.OsbLayers)
            {
                layersLayout.Add(new Label(Manager)
                {
                    StyleName = "listHeader",
                    Text = osbLayer.ToString(),
                });

                buildLayers(osbLayer, true);
                buildLayers(osbLayer, false);
            }
        }

        private void buildLayers(OsbLayer osbLayer, bool diffSpecific)
        {
            var layers = layerManager.FindLayers(l => l.OsbLayer == osbLayer && l.DiffSpecific == diffSpecific);

            var index = 0;
            foreach (var layer in layers)
            {
                var effect = layer.Effect;

                Widget layerRoot;
                Label nameLabel, detailsLabel;
                Button moveUpButton, moveDownButton, moveToTopButton, moveToBottomButton, diffSpecificButton, osbLayerButton, showHideButton;
                layersLayout.Add(layerRoot = new LinearLayout(Manager)
                {
                    AnchorFrom = BoxAlignment.Centre,
                    AnchorTo = BoxAlignment.Centre,
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
                                    AnchorFrom = BoxAlignment.Left,
                                    AnchorTo = BoxAlignment.Left,
                                },
                                detailsLabel = new Label(Manager)
                                {
                                    StyleName = "listItemSecondary",
                                    Text = getLayerDetails(layer, effect),
                                    AnchorFrom = BoxAlignment.Left,
                                    AnchorTo = BoxAlignment.Left,
                                },
                            },
                        },
                        diffSpecificButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = layer.DiffSpecific ? IconFont.FileO : IconFont.FilesO,
                            Tooltip = layer.DiffSpecific ? "Diff. specific\n(exports to .osu)" : "All diffs\n(exports to .osb)",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                        osbLayerButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.ThLarge,
                            Tooltip = "Osb Layer",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                        new LinearLayout(Manager)
                        {
                            StyleName = "condensed",
                            CanGrow = false,
                            Children = new Widget[]
                            {
                                moveUpButton = new Button(Manager)
                                {
                                    StyleName = "icon",
                                    Icon = IconFont.AngleUp,
                                    Tooltip = "Up",
                                    AnchorFrom = BoxAlignment.Centre,
                                    AnchorTo = BoxAlignment.Centre,
                                    CanGrow = false,
                                    Disabled = index == 0,
                                },
                                moveDownButton = new Button(Manager)
                                {
                                    StyleName = "icon",
                                    Icon = IconFont.AngleDown,
                                    Tooltip = "Down",
                                    AnchorFrom = BoxAlignment.Centre,
                                    AnchorTo = BoxAlignment.Centre,
                                    CanGrow = false,
                                    Disabled = index == layers.Count - 1,
                                },
                            },
                        },
                        new LinearLayout(Manager)
                        {
                            StyleName = "condensed",
                            CanGrow = false,
                            Children = new Widget[]
                            {
                                moveToTopButton = new Button(Manager)
                                {
                                    StyleName = "icon",
                                    Icon = IconFont.AngleDoubleUp,
                                    Tooltip = "Move to top",
                                    AnchorFrom = BoxAlignment.Centre,
                                    AnchorTo = BoxAlignment.Centre,
                                    CanGrow = false,
                                    Disabled = index == 0,
                                },
                                moveToBottomButton = new Button(Manager)
                                {
                                    StyleName = "icon",
                                    Icon = IconFont.AngleDoubleDown,
                                    Tooltip = "Move to bottom",
                                    AnchorFrom = BoxAlignment.Centre,
                                    AnchorTo = BoxAlignment.Centre,
                                    CanGrow = false,
                                    Disabled = index == layers.Count - 1,
                                },
                            },
                        },
                        showHideButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = layer.Visible ? IconFont.Eye : IconFont.EyeSlash,
                            Tooltip = "Show/Hide",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            Checkable = true,
                            Checked = layer.Visible,
                            CanGrow = false,
                        },
                    },
                });

                var la = layer;

                ChangedHandler changedHandler;
                EventHandler effectChangedHandler;
                layer.OnChanged += changedHandler = (sender, e) =>
                {
                    nameLabel.Text = la.Name;
                    diffSpecificButton.Icon = la.DiffSpecific ? IconFont.FileO : IconFont.FilesO;
                    diffSpecificButton.Tooltip = la.DiffSpecific ? "Diff. specific\n(exports to .osu)" : "All diffs\n(exports to .osb)";
                    showHideButton.Icon = la.Visible ? IconFont.Eye : IconFont.EyeSlash;
                    showHideButton.Checked = la.Visible;
                };
                effect.OnChanged += effectChangedHandler = (sender, e) => detailsLabel.Text = getLayerDetails(la, effect);
                layerRoot.OnHovered += (sender, e) =>
                {
                    la.Highlight = e.Hovered;
                    OnLayerPreselect?.Invoke(e.Hovered ? la : null);
                };
                layerRoot.OnClickDown += (sender, e) =>
                {
                    OnLayerSelected?.Invoke(la);
                    return true;
                };
                layerRoot.OnDisposed += (sender, e) =>
                {
                    la.Highlight = false;
                    la.OnChanged -= changedHandler;
                    effect.OnChanged -= effectChangedHandler;
                };

                moveUpButton.OnClick += (sender, e) => layerManager.MoveUp(la);
                moveDownButton.OnClick += (sender, e) => layerManager.MoveDown(la);
                moveToTopButton.OnClick += (sender, e) => layerManager.MoveToTop(la);
                moveToBottomButton.OnClick += (sender, e) => layerManager.MoveToBottom(la);
                diffSpecificButton.OnClick += (sender, e) => la.DiffSpecific = !la.DiffSpecific;
                osbLayerButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowContextMenu("Choose an osb layer", selectedOsbLayer => la.OsbLayer = selectedOsbLayer, Project.OsbLayers);
                showHideButton.OnValueChanged += (sender, e) => la.Visible = showHideButton.Checked;
                index++;
            }
        }

        private static string getLayerDetails(EditorStoryboardLayer layer, Effect effect)
            => layer.EstimatedSize > 40 * 1024 ?
                $"using {effect.BaseName} ({StringHelper.ToByteSize(layer.EstimatedSize)})" :
                $"using {effect.BaseName}";
    }
}
