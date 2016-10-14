using StorybrewEditor.Graphics.Textures;

namespace StorybrewEditor.Graphics.Text
{
    public class FontCharacter
    {
        private Texture2dSlice texture;
        public Texture2dSlice Texture => texture;
        public bool IsEmpty => texture == null;

        private int width;
        public int Width => width;

        private int height;
        public int Height => height;

        public FontCharacter(Texture2dSlice texture, int width, int height)
        {
            this.texture = texture;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
            => $"{texture} {width}x{height}";
    }
}
