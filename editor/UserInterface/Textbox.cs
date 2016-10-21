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
        private int cursorPosition;

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
                SetValueSilent(value);

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

        public void SetValueSilent(string value)
        {
            content.Text = value;
            if (cursorPosition > content.Text.Length)
                cursorPosition = content.Text.Length;
        }

        private bool acceptMultiline;
        public bool AcceptMultiline
        {
            get { return acceptMultiline; }
            set
            {
                if (acceptMultiline == value) return;
                acceptMultiline = value;

                if (!acceptMultiline)
                    Value = Value.Replace("\n", "");
            }
        }
        public bool EnterCommits = true;

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
                        if (cursorPosition > 0)
                        {
                            cursorPosition--;
                            Value = Value.Remove(cursorPosition, 1);
                        }
                        break;
                    case Key.Delete:
                        if (cursorPosition < Value.Length)
                            Value = Value.Remove(cursorPosition, 1);
                        break;
                    case Key.C:
                        if (manager.ScreenLayerManager.Editor.InputManager.ControlOnly)
                            System.Windows.Forms.Clipboard.SetText(Value, System.Windows.Forms.TextDataFormat.UnicodeText);
                        break;
                    case Key.V:
                        if (manager.ScreenLayerManager.Editor.InputManager.ControlOnly)
                        {
                            var clipboardText = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                            if (!AcceptMultiline)
                                clipboardText = clipboardText.Replace("\n", "");
                            Value = Value.Insert(cursorPosition, clipboardText);
                            cursorPosition += clipboardText.Length;
                        }
                        break;
                    case Key.X:
                        if (manager.ScreenLayerManager.Editor.InputManager.ControlOnly)
                        {
                            System.Windows.Forms.Clipboard.SetText(Value, System.Windows.Forms.TextDataFormat.UnicodeText);
                            Value = string.Empty;
                            cursorPosition = 0;
                        }
                        break;
                    case Key.Left:
                        if (cursorPosition > 0)
                            cursorPosition--;
                        break;
                    case Key.Right:
                        if (cursorPosition < Value.Length)
                            cursorPosition++;
                        break;
                    case Key.Enter:
                    case Key.KeypadEnter:
                        if (AcceptMultiline && (!EnterCommits || manager.ScreenLayerManager.Editor.InputManager.Shift))
                        {
                            Value = Value.Insert(cursorPosition, "\n");
                            cursorPosition++;
                        }
                        else if (EnterCommits && hasCommitPending)
                        {
                            OnValueCommited?.Invoke(this, EventArgs.Empty);
                            hasCommitPending = false;
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

                Value = Value.Insert(cursorPosition, e.KeyChar.ToString());
                cursorPosition++;
                return true;
            };
            OnClickDown += (sender, e) =>
            {
                manager.KeyboardFocus = this;
                cursorPosition = content.GetCharacterIndexAt(new Vector2(e.X, e.Y));
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
                var characterBounds = content.GetCharacterBounds(cursorPosition);
                var position = new Vector2(characterBounds.Left, characterBounds.Top + characterBounds.Height * 0.2f);
                var scale = new Vector2(Manager.PixelSize, characterBounds.Height * 0.6f);

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
