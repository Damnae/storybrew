using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.UserInterface.Drawables;
using StorybrewEditor.UserInterface.Skinning.Styles;
using System;

namespace StorybrewEditor.UserInterface
{
    public class ProgressBar : Widget
    {
        private Drawable bar = NullDrawable.Instance;

        public override Vector2 MinSize => bar.MinSize;
        public override Vector2 PreferredSize => new Vector2(Math.Max(150, bar.PreferredSize.X), Math.Max(32, bar.PreferredSize.Y));

        public float MinValue = 0;
        public float MaxValue = 1;

        private float value = 0.5f;
        public float Value
        {
            get { return value; }
            set
            {
                value = Math.Min(Math.Max(MinValue, value), MaxValue);

                if (this.value == value) return;
                this.value = value;
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetValueSilent(float value)
            => this.value = Math.Min(Math.Max(MinValue, value), MaxValue);

        public event EventHandler OnValueChanged;

        public ProgressBar(WidgetManager manager) : base(manager)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            bar = null;

            base.Dispose(disposing);
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<ProgressBarStyle>(StyleName);

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var progressBarStyle = (ProgressBarStyle)style;

            bar = progressBarStyle.Bar;
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);

            var progress = value / (MaxValue - MinValue);
            var minWidth = bar.MinSize.X;

            var barBounds = Bounds;
            barBounds.Right = barBounds.Left + minWidth + (barBounds.Width - minWidth) * progress;
            bar.Draw(drawContext, Manager.Camera, barBounds, actualOpacity);
        }
    }
}
