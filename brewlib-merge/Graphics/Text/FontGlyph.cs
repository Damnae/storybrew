using BrewLib.Graphics.Textures;
using OpenTK;

namespace BrewLib.Graphics.Text
{
    public class FontGlyph
    {
        readonly Texture2dRegion texture;
        public Texture2dRegion Texture => texture;
        public bool IsEmpty => texture == null;

        readonly int width, height;
        public int Width => width;
        public int Height => height;
        public Vector2 Size => new Vector2(width, height);

        public FontGlyph(Texture2dRegion texture, int width, int height)
        {
            this.texture = texture;
            this.width = width;
            this.height = height;
        }
        public override string ToString() => $"{texture} {width}x{height}";
    }
}