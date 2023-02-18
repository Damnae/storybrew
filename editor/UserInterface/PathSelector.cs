using BrewLib.UserInterface;
using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using OpenTK;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.UserInterface.Skinning.Styles;
using System;

namespace StorybrewEditor.UserInterface
{
    public class PathSelector : Widget
    {
        readonly PathSelectorMode mode;

        readonly LinearLayout layout;
        readonly Textbox textbox;
        readonly Button button;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public string LabelText { get => textbox.LabelText; set => textbox.LabelText = value; }
        public string Value { get => textbox.Value; set => textbox.Value = value; }

        public string Filter = "All files (*.*)|*.*";
        public string SaveExtension = "";

        public event EventHandler OnValueChanged, OnValueCommited;

        public PathSelector(WidgetManager manager, PathSelectorMode mode) : base(manager)
        {
            this.mode = mode;

            Add(layout = new LinearLayout(manager)
            {
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Horizontal = true,
                Fill = true,
                FitChildren = true,
                Children = new Widget[]
                {
                    textbox = new Textbox(manager)
                    {
                        AnchorFrom = BoxAlignment.BottomLeft,
                        AnchorTo = BoxAlignment.BottomLeft
                    },
                    button = new Button(manager)
                    {
                        Icon = IconFont.FolderOpen,
                        Tooltip = "Browse",
                        AnchorFrom = BoxAlignment.BottomRight,
                        AnchorTo = BoxAlignment.BottomRight,
                        CanGrow = false
                    }
                }
            });

            textbox.OnValueChanged += (sender, e) => OnValueChanged?.Invoke(this, EventArgs.Empty);
            textbox.OnValueCommited += (sender, e) => OnValueCommited?.Invoke(this, EventArgs.Empty);
            button.OnClick += (sender, e) =>
            {
                switch (mode)
                {
                    case PathSelectorMode.Folder:
                        Manager.ScreenLayerManager.OpenFolderPicker(LabelText, textbox.Value, (path) => textbox.Value = path);
                        break;

                    case PathSelectorMode.OpenFile:
                        Manager.ScreenLayerManager.OpenFilePicker(LabelText, textbox.Value, null, Filter, (path) => textbox.Value = path);
                        break;

                    case PathSelectorMode.OpenDirectory:
                        Manager.ScreenLayerManager.OpenFilePicker(LabelText, "", textbox.Value, Filter, (path) => textbox.Value = path);
                        break;

                    case PathSelectorMode.SaveFile:
                        Manager.ScreenLayerManager.OpenSaveLocationPicker(LabelText, textbox.Value, SaveExtension, Filter, (path) => textbox.Value = path);
                        break;
                }
            };
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<PathSelectorStyle>(BuildStyleName());

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var pathSelectorStyle = (PathSelectorStyle)style;

            layout.StyleName = pathSelectorStyle.LinearLayoutStyle;
            textbox.StyleName = pathSelectorStyle.TextboxStyle;
            button.StyleName = pathSelectorStyle.ButtonStyle;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
    public enum PathSelectorMode
    {
        Folder, OpenFile, OpenDirectory, SaveFile
    }
}