using BrewLib.UserInterface;
using BrewLib.UserInterface.Skinning.Styles;
using OpenTK;
using StorybrewCommon.Util;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.UserInterface.Skinning.Styles;
using System;

namespace StorybrewEditor.UserInterface
{
    public class Selectbox : Widget, Field
    {
        private Button button;

        public override Vector2 MinSize => button.MinSize;
        public override Vector2 MaxSize => button.MaxSize;
        public override Vector2 PreferredSize => button.PreferredSize;

        private NamedValue[] options;
        public NamedValue[] Options
        {
            get { return options; }
            set
            {
                if (options == value) return;
                options = value;

                button.Text = findValueName(this.value);
            }
        }

        private object value;
        public object Value
        {
            get { return value; }
            set
            {
                if (this.value == value) return;
                this.value = value;

                button.Text = findValueName(this.value);
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get { return Value; }
            set { Value = value; }
        }

        public event EventHandler OnValueChanged;

        public Selectbox(WidgetManager manager) : base(manager)
        {
            Add(button = new Button(manager));
            button.OnClick += (sender, e) =>
            {
                if (options == null)
                    return;
                else if (options.Length > 2)
                    Manager.ScreenLayerManager.ShowContextMenu("Select a value", optionValue => Value = optionValue.Value, options);
                else
                {
                    var optionFound = false;
                    foreach (var option in options)
                    {
                        if (optionFound)
                        {
                            Value = option.Value;
                            optionFound = false;
                            break;
                        }
                        else if (option.Value.Equals(value))
                            optionFound = true;
                    }
                    if (optionFound)
                        Value = options[0].Value;
                }
            };
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<SelectboxStyle>(BuildStyleName());

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var selectboxStyle = (SelectboxStyle)style;

            button.StyleName = selectboxStyle.ButtonStyle;
        }

        protected override void Layout()
        {
            base.Layout();
            button.Size = Size;
        }

        private string findValueName(object value)
        {
            if (options == null) return string.Empty;
            foreach (var option in options)
            {
                if (option.Value.Equals(value))
                    return option.Name;
            }
            return string.Empty;
        }
    }
}
