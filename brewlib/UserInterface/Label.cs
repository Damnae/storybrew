using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Drawables;
using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using OpenTK;
using System;

namespace BrewLib.UserInterface
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
                var position = AbsolutePosition;
                var size = Size;
                var textSize = new Vector2(Math.Min(textDrawable.Size.X, size.X), Math.Min(textDrawable.Size.Y, size.Y));

                var alignment = textDrawable.Alignment;
                if (alignment.HasFlag(BoxAlignment.Right))
                    position.X += size.X - textSize.X;
                else if (!alignment.HasFlag(BoxAlignment.Left))
                    position.X += size.X * 0.5f - textSize.X * 0.5f;
                if (alignment.HasFlag(BoxAlignment.Bottom))
                    position.Y += size.Y - textSize.Y;
                else if (!alignment.HasFlag(BoxAlignment.Top))
                    position.Y += size.Y * 0.5f - textSize.Y * 0.5f;

                position = Manager.SnapToPixel(position);
                return new Box2(position, position + textSize);
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
            if (!string.IsNullOrWhiteSpace(Text))
            {
                textDrawable.Draw(drawContext, Manager.Camera, TextBounds, actualOpacity);
#if DEBUG
                Manager.Skin.GetDrawable("debug_textbounds")?.Draw(drawContext, Manager.Camera, TextBounds, 1);
#endif
            }
        }

        public Box2 GetCharacterBounds(int index)
        {
            var position = AbsolutePosition;
            var bounds = textDrawable.GetCharacterBounds(index);
            return new Box2(position.X + bounds.Left, position.Y + bounds.Top, position.X + bounds.Right, position.Y + bounds.Bottom);
        }

        public void ForTextBounds(int startIndex, int endIndex, Action<Box2> action)
        {
            var position = AbsolutePosition;
            textDrawable.ForTextBounds(startIndex, endIndex, bounds =>
                action(new Box2(position.X + bounds.Left, position.Y + bounds.Top, position.X + bounds.Right, position.Y + bounds.Bottom)));
        }

        public int GetCharacterIndexAt(Vector2 position)
            => textDrawable.GetCharacterIndexAt(position - AbsolutePosition);

        public int GetCharacterIndexAbove(int index)
            => textDrawable.GetCharacterIndexAbove(index);

        public int GetCharacterIndexBelow(int index)
            => textDrawable.GetCharacterIndexBelow(index);
    }
}
