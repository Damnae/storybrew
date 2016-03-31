using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.UserInterface.Drawables;
using StorybrewEditor.UserInterface.Skinning.Styles;

namespace StorybrewEditor.UserInterface
{
    public class Image : Widget
    {
        private Sprite sprite = new Sprite();
        
        public override Vector2 PreferredSize => sprite.Texture?.Size ?? Vector2.Zero;

        public Texture2d Texture { get { return sprite.Texture; } set { sprite.Texture = value; } }

        public Image(WidgetManager manager) : base(manager)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sprite?.Dispose();
            }
            sprite = null;

            base.Dispose(disposing);
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<ImageStyle>(StyleName);

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var imageStyle = (ImageStyle)style;

            sprite.Color = imageStyle.Color;
            sprite.ScaleMode = imageStyle.ScaleMode;
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);
            sprite.Draw(drawContext, Manager.Camera, Bounds, actualOpacity);
        }
    }
}
