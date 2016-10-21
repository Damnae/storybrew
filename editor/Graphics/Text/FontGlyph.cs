using StorybrewEditor.Graphics.Textures;

namespace StorybrewEditor.Graphics.Text
{
    public class FontGlyph
    {
        private Texture2dRegion texture;
        public Texture2dRegion Texture => texture;
        public bool IsEmpty => texture == null;

        private int width;
        public int Width => width;

        private int height;
        public int Height => height;

        public FontGlyph(Texture2dRegion texture, int width, int height)
        {
            this.texture = texture;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
            => $"{texture} {width}x{height}";
    }
}
