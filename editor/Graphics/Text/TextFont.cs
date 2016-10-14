using System;

namespace StorybrewEditor.Graphics.Text
{
    public interface TextFont : IDisposable
    {
        string Name { get; }
        float Size { get; }

        FontCharacter GetCharacter(char c);
    }
}
