using OpenTK;
using StorybrewEditor.UserInterface.Skinning.Styles;
using StorybrewEditor.Util;
using System;

namespace StorybrewEditor.UserInterface
{
    public class Button : Widget
    {
        private Label label;
        private ClickBehavior clickBehavior;

        public override Vector2 MinSize => new Vector2(label.MinSize.X + padding.Horizontal, label.MinSize.Y + padding.Vertical);
        public override Vector2 PreferredSize => new Vector2(label.PreferredSize.X + padding.Horizontal, label.PreferredSize.Y + padding.Vertical);

        public string Text { get { return label.Text; } set { label.Text = value; } }
        public IconFont Icon { get { return label.Icon; } set { label.Icon = value; } }

        private FourSide padding;
        public FourSide Padding
        {
            get { return padding; }
            set
            {
                if (padding == value) return;
                padding = value;
                InvalidateAncestorLayout();
            }
        }

        private bool isCheckable;
        public bool Checkable
        {
            get { return isCheckable; }
            set
            {
                if (isCheckable == value) return;
                isCheckable = value;
                if (!isCheckable) Checked = false;
            }
        }

        private bool isChecked;
        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                RefreshStyle();
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public bool Disabled
        {
            get { return clickBehavior.Disabled; }
            set { clickBehavior.Disabled = value; }
        }

        public event EventHandler OnClick;
        public event EventHandler OnValueChanged;

        public Button(WidgetManager manager) : base(manager)
        {
            Add(label = new Label(manager)
            {
                AnchorFrom = UiAlignment.Centre,
                AnchorTo = UiAlignment.Centre,
                Hoverable = false,
            });

            clickBehavior = new ClickBehavior(this);
            clickBehavior.OnStateChanged += (sender, e) => RefreshStyle();
            clickBehavior.OnClick += (sender, e) => Click();
        }

        public void BindToSetting<T>(Setting<T> setting, Action changedAction)
        {
            OnValueChanged += (sender, e) => setting.Set(Checked);
            EventHandler handler;
            setting.OnValueChanged += handler = (sender, e) =>
            {
                Checked = (bool)Convert.ChangeType((T)setting, typeof(bool));
                changedAction();
            };
            OnDisposed += (sender, e) => setting.OnValueChanged -= handler;
            handler(this, EventArgs.Empty);
        }

        public void Click()
        {
            if (isCheckable)
                Checked = !Checked;

            OnClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clickBehavior.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<ButtonStyle>(BuildStyleName(clickBehavior.Disabled ? "disabled" : null, clickBehavior.Hovered ? "hover" : null, clickBehavior.Pressed || isChecked ? "pressed" : null));

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var buttonStyle = (ButtonStyle)style;

            Padding = buttonStyle.Padding;
            label.StyleName = buttonStyle.LabelStyle;
            label.Offset = buttonStyle.LabelOffset;
        }

        protected override void Layout()
        {
            base.Layout();
            label.Size = new Vector2(Size.X - padding.Horizontal, Size.Y - padding.Vertical);
        }
    }
}
