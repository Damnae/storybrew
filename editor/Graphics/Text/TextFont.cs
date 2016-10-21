using System;

namespace StorybrewEditor.Graphics.Text
{
    public interface TextFont : IDisposable
    {
        string Name { get; }
        float Size { get; }
        int LineHeight { get; }

        FontGlyph GetGlyph(char c);
    }
}
