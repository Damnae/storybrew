using OpenTK;
using BrewLib.Graphics.Cameras;
using System;

namespace BrewLib.Graphics.Drawables
{
    public interface Drawable : IDisposable
    {
        Vector2 MinSize { get; }
        Vector2 PreferredSize { get; }

        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1);
    }
}
