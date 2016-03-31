using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.UserInterface.Skinning;
using System;

namespace StorybrewEditor.UserInterface.Drawables
{
    public interface Drawable : Skinnable, IDisposable
    {
        Vector2 MinSize { get; }
        Vector2 PreferredSize { get; }

        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1);
    }
}
