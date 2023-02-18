using BrewLib.Graphics.Cameras;
using OpenTK;

namespace BrewLib.Graphics.Drawables
{
    public class NullDrawable : Drawable
    {
        public static readonly Drawable Instance = new NullDrawable();

        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => Vector2.Zero;

        NullDrawable() { }
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1) { }
        public void Dispose() { }
    }
}