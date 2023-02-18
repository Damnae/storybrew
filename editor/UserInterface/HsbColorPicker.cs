using BrewLib.Graphics;
using BrewLib.Graphics.Drawables;
using BrewLib.UserInterface;
using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.UserInterface.Skinning.Styles;
using System;
using System.Drawing;

namespace StorybrewEditor.UserInterface
{
    public class HsbColorPicker : Widget, Field
    {
        Sprite previewSprite;
        readonly LinearLayout layout;
        readonly Slider hueSlider, saturationSlider, brightnessSlider, alphaSlider;
        readonly Textbox htmlTextbox;

        public override Vector2 MinSize => new Vector2(layout.MinSize.X, layout.MinSize.Y + previewHeight);
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => new Vector2(layout.PreferredSize.X, layout.PreferredSize.Y + previewHeight);

        Color4 value;
        public Color4 Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;

                updateWidgets();
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get => Value;
            set => Value = (Color4)value;
        }

        float previewHeight = 24;
        public float PreviewHeight
        {
            get => previewHeight;
            set
            {
                previewHeight = value;
                InvalidateAncestorLayout();
            }
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public HsbColorPicker(WidgetManager manager) : base(manager)
        {
            previewSprite = new Sprite
            {
                Texture = DrawState.WhitePixel,
                ScaleMode = ScaleMode.Fill
            };
            Add(layout = new LinearLayout(manager)
            {
                StyleName = "condensed",
                FitChildren = true,
                Children = new Widget[]
                {
                    new Label(manager)
                    {
                        StyleName = "small",
                        Text = "Hue"
                    },
                    hueSlider = new Slider(manager)
                    {
                        StyleName = "small",
                        MinValue = 0,
                        MaxValue = 1,
                        Value = 0
                    },
                    new Label(manager)
                    {
                        StyleName = "small",
                        Text = "Saturation"
                    },
                    saturationSlider = new Slider(manager)
                    {
                        StyleName = "small",
                        MinValue = 0,
                        MaxValue = 1,
                        Value = 0.7f
                    },
                    new Label(manager)
                    {
                        StyleName = "small",
                        Text = "Brightness"
                    },
                    brightnessSlider = new Slider(manager)
                    {
                        StyleName = "small",
                        MinValue = 0,
                        MaxValue = 1,
                        Value = 1
                    },
                    new Label(manager)
                    {
                        StyleName = "small",
                        Text = "Alpha"
                    },
                    alphaSlider = new Slider(manager)
                    {
                        StyleName = "small",
                        MinValue = 0,
                        MaxValue = 1,
                        Value = 1
                    },
                    htmlTextbox = new Textbox(manager)
                    {
                        EnterCommits = true
                    }
                }
            });
            updateWidgets();

            hueSlider.OnValueChanged += slider_OnValueChanged;
            saturationSlider.OnValueChanged += slider_OnValueChanged;
            brightnessSlider.OnValueChanged += slider_OnValueChanged;
            alphaSlider.OnValueChanged += slider_OnValueChanged;

            hueSlider.OnValueCommited += slider_OnValueCommited;
            saturationSlider.OnValueCommited += slider_OnValueCommited;
            brightnessSlider.OnValueCommited += slider_OnValueCommited;
            alphaSlider.OnValueCommited += slider_OnValueCommited;
            htmlTextbox.OnValueCommited += htmlTextbox_OnValueCommited;
        }

        void slider_OnValueChanged(object sender, EventArgs e) => Value = Color4.FromHsv(new Vector4(
            hueSlider.Value % 1f, saturationSlider.Value, brightnessSlider.Value, alphaSlider.Value));

        void slider_OnValueCommited(object sender, EventArgs e) => OnValueCommited?.Invoke(this, EventArgs.Empty);

        void htmlTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var htmlColor = htmlTextbox.Value.Trim();
            if (!htmlColor.StartsWith("#")) htmlColor = "#" + htmlColor;

            Color color;
            try
            {
                color = ColorTranslator.FromHtml(htmlColor);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Color4(color.R / 255f, color.G / 255f, color.B / 255f, alphaSlider.Value);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            previewSprite.Color = value;

            var hsba = value.ToHsba();
            if (hsba.Z > 0)
            {
                if (!float.IsNaN(hsba.X))
                {
                    hueSlider.SetValueSilent(hsba.X);
                    hueSlider.Tooltip = $"{hueSlider.Value * 360:F0}°";
                    hueSlider.Disabled = false;
                }
                else
                {
                    hueSlider.Tooltip = null;
                    hueSlider.Disabled = true;
                }

                saturationSlider.SetValueSilent(hsba.Y);
                saturationSlider.Tooltip = $"{saturationSlider.Value:.%}";
                saturationSlider.Disabled = false;
            }
            else
            {
                hueSlider.Tooltip = null;
                hueSlider.Disabled = true;

                saturationSlider.Tooltip = null;
                saturationSlider.Disabled = true;
            }

            brightnessSlider.SetValueSilent(hsba.Z);
            brightnessSlider.Tooltip = $"{brightnessSlider.Value:.%}";

            alphaSlider.SetValueSilent(hsba.W);
            alphaSlider.Tooltip = $"{alphaSlider.Value:.%}";

            htmlTextbox.SetValueSilent(ColorTranslator.ToHtml(Color.FromArgb(value.ToArgb())));
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<ColorPickerStyle>(BuildStyleName());

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var textboxStyle = (ColorPickerStyle)style;
        }
        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);

            var bounds = Bounds;
            previewSprite.Draw(drawContext, Manager.Camera, new Box2(bounds.Left, bounds.Top, bounds.Right, bounds.Top + previewHeight), actualOpacity);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) previewSprite.Dispose();
            previewSprite = null;
            base.Dispose(disposing);
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Offset = new Vector2(0, previewHeight);
            layout.Size = new Vector2(Size.X, Size.Y - previewHeight);
        }
    }
}