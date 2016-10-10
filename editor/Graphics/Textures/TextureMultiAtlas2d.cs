﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace StorybrewEditor.Graphics.Textures
{
    public class TextureMultiAtlas2d : IDisposable
    {
        private Stack<TextureAtlas2d> atlases = new Stack<TextureAtlas2d>();

        private int width;
        private int height;
        private string description;

        public TextureMultiAtlas2d(int width, int height, string description)
        {
            this.width = width;
            this.height = height;
            this.description = description;
            pushAtlas();
        }

        /// <summary>
        /// Adds a bitmap to the atlas and returns the new slice
        /// </summary>
        public Texture2dSlice AddSlice(Bitmap bitmap, string description)
        {
            if (bitmap.Width > width || bitmap.Height > height)
                throw new InvalidOperationException("Bitmap doesn't fit in this atlas");

            var atlas = atlases.Peek();
            var slice = atlas.AddSlice(bitmap, description);
            if (slice == null)
            {
                Debug.Print($"{this.description} is full, adding an atlas");
                atlas = pushAtlas();
                slice = atlas.AddSlice(bitmap, description);
            }
            return slice;
        }

        private TextureAtlas2d pushAtlas()
        {
            var atlas = new TextureAtlas2d(width, height, $"{description} #{atlases.Count + 1}");
            atlases.Push(atlas);
            return atlas;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    while (atlases.Count > 0)
                        atlases.Pop().Dispose();
                }
                atlases = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
