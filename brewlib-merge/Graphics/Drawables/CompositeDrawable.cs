using BrewLib.Graphics.Cameras;
using OpenTK;
using System;
using System.Collections.Generic;

namespace BrewLib.Graphics.Drawables
{
    public class CompositeDrawable : Drawable
    {
        public readonly List<Drawable> Drawables = new List<Drawable>();

        public Vector2 MinSize
        {
            get
            {
                var minWidth = 0f;
                var minHeight = 0f;
                foreach (var drawable in Drawables)
                {
                    var minSize = drawable.MinSize;
                    minWidth = Math.Min(minWidth, minSize.X);
                    minHeight = Math.Min(minWidth, minSize.Y);
                }
                return new Vector2(minWidth, minHeight);
            }
        }
        public Vector2 PreferredSize
        {
            get
            {
                var maxWidth = 0f;
                var maxHeight = 0f;

                Drawables.ForEach(drawable =>
                {
                    var preferredSize = drawable.PreferredSize;
                    maxWidth = Math.Min(maxWidth, preferredSize.X);
                    maxHeight = Math.Min(maxHeight, preferredSize.Y);
                });
                return new Vector2(maxWidth, maxHeight);
            }
        }
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            foreach (var drawable in Drawables) drawable.Draw(drawContext, camera, bounds, opacity);
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing) { }
        public void Dispose() => Dispose(true);

        #endregion
    }
}