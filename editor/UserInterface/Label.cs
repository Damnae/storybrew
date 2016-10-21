using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Drawables;
using StorybrewEditor.UserInterface.Skinning.Styles;
using StorybrewEditor.Util;

namespace StorybrewEditor.UserInterface
{
    public class Label : Widget
    {
        private TextDrawable textDrawable = new TextDrawable();

        public override Vector2 MinSize => new Vector2(0, PreferredSize.Y);
        public override Vector2 PreferredSize => textDrawable.Size;

        public string Text { get { return textDrawable.Text; } set { if (textDrawable.Text == value) return; textDrawable.Text = value; InvalidateAncestorLayout(); } }
        public IconFont Icon { get { return textDrawable.Icon; } set { if (textDrawable.Icon == value) return; textDrawable.Icon = value; InvalidateAncestorLayout(); } }

        public Box2 TextBounds
        {
            get
            {
                var position = ScreenPosition;
                var alignment = textDrawable.Alignment;

                if (alignment.HasFlag(UiAlignment.Right))
                    position.X += Size.X - textDrawable.Size.X;
                else if (!alignment.HasFlag(UiAlignment.Left))
                    position.X += Size.X * 0.5f - textDrawable.Size.X * 0.5f;
                if (alignment.HasFlag(UiAlignment.Bottom))
                    position.Y += Size.Y - textDrawable.Size.Y;
                else if (!alignment.HasFlag(UiAlignment.Top))
                    position.Y += Size.Y * 0.5f - textDrawable.Size.Y * 0.5f;

                position = Manager.SnapToPixel(position);
                return new Box2(position, position + textDrawable.Size);
            }
        }

        public Label(WidgetManager manager) : base(manager)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                textDrawable?.Dispose();
            }
            textDrawable = null;

            base.Dispose(disposing);
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<LabelStyle>(StyleName);

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var labelStyle = (LabelStyle)style;

            textDrawable.FontName = labelStyle.FontName;
            textDrawable.FontSize = labelStyle.FontSize;
            textDrawable.Alignment = labelStyle.TextAlignment;
            textDrawable.Trimming = labelStyle.Trimming;
            textDrawable.Color = labelStyle.Color;
        }

        public override void PreLayout()
        {
            base.PreLayout();

            var scalingChanged = false;

            var camera = Manager.Camera as CameraOrtho;
            var scaling = camera?.HeightScaling ?? 1;
            if (scaling != 0 && textDrawable.Scaling != scaling)
            {
                textDrawable.Scaling = scaling;
                scalingChanged = true;
            }

            if (NeedsLayout || scalingChanged)
            {
                textDrawable.MaxSize = Vector2.Zero;
                InvalidateAncestorLayout();
            }
        }

        protected override void Layout()
        {
            base.Layout();

            if (textDrawable.MaxSize != Size)
            {
                textDrawable.MaxSize = Size;
                InvalidateAncestorLayout();
            }
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);
            textDrawable.Draw(drawContext, Manager.Camera, TextBounds, actualOpacity);
        }

        public Box2 GetCharacterBounds(int index)
        {
            var position = ScreenPosition;
            var bounds = textDrawable.GetCharacterBounds(index);

            return new Box2(position.X + bounds.Left, position.Y + bounds.Top, position.X + bounds.Right, position.Y + bounds.Bottom);
        }

        public int GetCharacterIndexAt(Vector2 position)
            => textDrawable.GetCharacterIndexAt(position - ScreenPosition);
    }
}
