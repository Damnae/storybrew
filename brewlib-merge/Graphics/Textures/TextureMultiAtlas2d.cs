using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace BrewLib.Graphics.Textures
{
    public class TextureMultiAtlas2d : IDisposable
    {
        Stack<TextureAtlas2d> atlases = new Stack<TextureAtlas2d>();
        List<Texture2d> oversizeTextures;

        readonly int width, height, padding;
        readonly string description;
        readonly TextureOptions textureOptions;

        public TextureMultiAtlas2d(int width, int height, string description, TextureOptions textureOptions = null, int padding = 0)
        {
            this.width = width;
            this.height = height;
            this.description = description;
            this.textureOptions = textureOptions;
            this.padding = padding;
            pushAtlas();
        }

        public Texture2dRegion AddRegion(Bitmap bitmap, string description)
        {
            if (bitmap.Width > width || bitmap.Height > height)
            {
                Trace.WriteLine($"Bitmap \"{description}\" doesn't fit in this atlas");

                var texture = Texture2d.Load(bitmap, description, textureOptions);
                (oversizeTextures ?? (oversizeTextures = new List<Texture2d>())).Add(texture);
                return texture;
            }

            var atlas = atlases.Peek();
            var region = atlas.AddRegion(bitmap, description);
            if (region == null)
            {
                Trace.WriteLine($"{this.description} is full, adding an atlas");
                atlas = pushAtlas();
                region = atlas.AddRegion(bitmap, description);
            }
            return region;
        }

        TextureAtlas2d pushAtlas()
        {
            var atlas = new TextureAtlas2d(width, height, $"{description} #{atlases.Count + 1}", textureOptions, padding);
            atlases.Push(atlas);
            return atlas;
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    while (atlases.Count > 0) atlases.Pop().Dispose();
                    if (oversizeTextures != null) foreach (var texture in oversizeTextures) texture.Dispose();
                }
                atlases = null;
                oversizeTextures = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}