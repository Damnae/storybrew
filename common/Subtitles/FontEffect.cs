using OpenTK;
using System.Drawing;
using Tiny;

namespace StorybrewCommon.Subtitles
{
    public interface FontEffect
    {
        bool Overlay { get; }

        Vector2 Measure();
        void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y);

        bool Matches(TinyToken cachedEffectRoot);
        TinyObject ToTinyObject();
    }
}