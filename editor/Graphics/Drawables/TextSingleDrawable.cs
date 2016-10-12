using OpenTK;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.Util;

namespace StorybrewEditor.Graphics.Drawables
{
    public class TextSingleDrawable : TextDrawable
    {
        private Texture2d texture;
        private Vector2 textureMaxSize;
        private float textureScaling = 1;
        private Vector2 measuredSize;
        private bool textureMeasured;

        protected override Vector2 TextureMaxSize => textureMaxSize;
        protected override float TextureScaling => textureScaling;

        public override Vector2 Size
        {
            get
            {
                ValidateMeasuredSize();
                return measuredSize;
            }
        }

        protected override void DrawText(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            DrawState.Prepare(drawContext.SpriteRenderer, camera, RenderStates)
                .Draw(texture, bounds.Left, bounds.Top, 0, 0, 1 / Scaling, 1 / Scaling, 0, Color.WithOpacity(opacity));
        }

        protected override void ValidateMeasuredSize()
        {
            if (textureMeasured) return;
            textureMeasured = true;

            DrawState.FontManager.CreateBitmap(Text, FontName, FontSize * Scaling, MaxSize * Scaling, Vector2.Zero, Alignment, Trimming, out measuredSize, true);
            measuredSize /= Scaling;
        }

        protected override void InvalidateMeasuredSize()
        {
            textureMeasured = false;
        }

        protected override void ValidateTexture()
        {
            if (texture != null) return;
            textureMeasured = true;
            textureMaxSize = MaxSize;
            textureScaling = Scaling;

            texture = DrawState.FontManager.CreateTexture(Text, FontName, FontSize * Scaling, MaxSize * Scaling, Vector2.Zero, Alignment, Trimming, out measuredSize);
            measuredSize /= Scaling;
        }

        protected override void InvalidateTexture()
        {
            texture?.Dispose();
            texture = null;
            textureMaxSize = Vector2.Zero;
            InvalidateMeasuredSize();
        }

        #region IDisposable Support

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                texture?.Dispose();
            }
            texture = null;
        }

        #endregion
    }
}
