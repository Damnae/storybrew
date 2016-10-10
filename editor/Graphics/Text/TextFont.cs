using OpenTK;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.UserInterface;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace StorybrewEditor.Graphics.Text
{
    public class TextFont : IDisposable
    {
        private Dictionary<char, Character> characters = new Dictionary<char, Character>();
        private TextureMultiAtlas2d atlas;

        private string name;
        public string Name => name;

        private float size;
        public float Size => size;

        public TextFont(string name, float size)
        {
            this.name = name;
            this.size = size;
        }

        public virtual Character GetCharacter(char c)
        {
            Character character;
            if (!characters.TryGetValue(c, out character))
                characters.Add(c, character = generateCharacter(c));
            return character;
        }

        private Character generateCharacter(char c)
        {
            Vector2 measuredSize;
            if (char.IsWhiteSpace(c))
            {
                DrawState.FontManager.CreateBitmap(c.ToString(), name, size,
                    Vector2.Zero, Vector2.Zero, UiAlignment.Centre, StringTrimming.None, out measuredSize, true);
                return new Character(null, (int)measuredSize.X, (int)measuredSize.Y);
            }
            else
            {
                atlas = atlas ?? new TextureMultiAtlas2d(512, 512, $"Font Atlas {name} {size}x");
                var bitmap = DrawState.FontManager.CreateBitmap(c.ToString(), name, size,
                    Vector2.Zero, Vector2.Zero, UiAlignment.Centre, StringTrimming.None, out measuredSize, false);
                var texture = atlas.AddSlice(bitmap, $"character:{c}@{Name}:{Size}");
                return new Character(texture, (int)measuredSize.X, (int)measuredSize.Y);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var character in characters.Values)
                        character.Texture?.Dispose();
                    atlas?.Dispose();
                }
                characters = null;
                atlas = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public class Character
        {
            private Texture2dSlice texture;
            public Texture2dSlice Texture => texture;
            public bool IsEmpty => texture == null;

            private int baseWidth;
            public int BaseWidth => baseWidth;

            private int baseHeight;
            public int BaseHeight => baseHeight;

            public Character(Texture2dSlice texture, int baseWidth, int baseHeight)
            {
                this.texture = texture;
                this.baseWidth = baseWidth;
                this.baseHeight = baseHeight;
            }

            public override string ToString()
                => $"{texture} {baseWidth}x{baseHeight}";
        }
    }
}
