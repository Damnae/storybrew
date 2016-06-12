using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Drawables;
using StorybrewEditor.UserInterface.Skinning.Styles;
using StorybrewEditor.Util;
using System;

namespace StorybrewEditor.UserInterface
{
    public class Textbox : Widget, Field
    {
        private Label label;
        private Label content;
        private Sprite cursorLine;
        private bool hasFocus;
        private bool hovered;
        private bool hasCommitPending;

        public override Vector2 MinSize => new Vector2(0, PreferredSize.Y);
        public override Vector2 MaxSize => new Vector2(0, PreferredSize.Y);
        public override Vector2 PreferredSize
        {
            get
            {
                var contentSize = content.PreferredSize;
                if (string.IsNullOrWhiteSpace(label.Text))
                    return new Vector2(Math.Max(contentSize.X, 200), contentSize.Y);

                var labelSize = label.PreferredSize;
                return new Vector2(Math.Max(labelSize.X, 200), labelSize.Y + contentSize.Y);
            }
        }

        public string LabelText { get { return label.Text; } set { label.Text = value; } }

        public string Value
        {
            get { return content.Text; }
            set
            {
                if (content.Text == value) return;
                content.Text = value;

                if (hasFocus) hasCommitPending = true;
                OnValueChanged?.Invoke(this, EventArgs.Empty);
                if (!hasFocus) OnValueCommited?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get { return Value; }
            set { Value = (string)value; }
        }

        public event EventHandler OnValueChanged;
        public event EventHandler OnValueCommited;

        public Textbox(WidgetManager manager) : base(manager)
        {
            cursorLine = new Sprite()
            {
                Texture = DrawState.WhitePixel,
                ScaleMode = ScaleMode.Fill,
            };

            Add(content = new Label(manager)
            {
                AnchorFrom = UiAlignment.BottomLeft,
                AnchorTo = UiAlignment.BottomLeft,
            });
            Add(label = new Label(manager)
            {
                AnchorFrom = UiAlignment.TopLeft,
                AnchorTo = UiAlignment.TopLeft,
            });

            OnFocusChange += (sender, e) =>
            {
                if (hasFocus == e.HasFocus) return;
                if (hasFocus && hasCommitPending)
                {
                    OnValueCommited?.Invoke(this, EventArgs.Empty);
                    hasCommitPending = false;
                }

                hasFocus = e.HasFocus;
                RefreshStyle();
            };
            OnHovered += (sender, e) =>
            {
                hovered = e.Hovered;
                RefreshStyle();
            };
            OnKeyDown += (sender, e) =>
            {
                if (!hasFocus) return false;

                switch (e.Key)
                {
                    case Key.Escape:
                        if (hasFocus)
                            manager.KeyboardFocus = null;
                        break;
                    case Key.BackSpace:
                        if (Value.Length > 0)
                            Value = Value.Substring(0, Value.Length - 1);
                        break;
                    case Key.C:
                        if (manager.ScreenLayerManager.Editor.InputManager.ControlOnly)
                            System.Windows.Forms.Clipboard.SetText(Value, System.Windows.Forms.TextDataFormat.UnicodeText);
                        break;
                    case Key.V:
                        if (manager.ScreenLayerManager.Editor.InputManager.ControlOnly)
                            Value += System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                        break;
                    case Key.X:
                        if (manager.ScreenLayerManager.Editor.InputManager.ControlOnly)
                        {
                            System.Windows.Forms.Clipboard.SetText(Value, System.Windows.Forms.TextDataFormat.UnicodeText);
                            Value = string.Empty;
                        }
                        break;
                }
                return true;
            };
            OnKeyUp += (sender, e) =>
            {
                return hasFocus;
            };
            OnKeyPress += (sender, e) =>
            {
                if (!hasFocus) return false;

                Value += e.KeyChar;
                return true;
            };
            OnClickDown += (sender, e) =>
            {
                manager.KeyboardFocus = this;
                return true;
            };
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<TextboxStyle>(BuildStyleName(hovered ? "hover" : null, hasFocus ? "focus" : null));

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var textboxStyle = (TextboxStyle)style;

            label.StyleName = textboxStyle.LabelStyle;
            content.StyleName = textboxStyle.ContentStyle;
        }

        protected override void DrawForeground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawForeground(drawContext, actualOpacity);

            if (hasFocus)
            {
                var contentBounds = content.TextBounds;
                var position = new Vector2(string.IsNullOrEmpty(Value) ? contentBounds.Left : contentBounds.Right, contentBounds.Top + content.TextBounds.Height * 0.2f);
                var scale = new Vector2(Manager.PixelSize, content.TextBounds.Height * 0.6f);

                cursorLine.Color = Color4.White;
                cursorLine.Draw(drawContext, Manager.Camera, new Box2(position, position + scale), actualOpacity);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cursorLine.Dispose();
            }
            cursorLine = null;

            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            content.Size = new Vector2(Size.X, content.PreferredSize.Y);
            label.Size = new Vector2(Size.X, label.PreferredSize.Y);
        }
    }
}
