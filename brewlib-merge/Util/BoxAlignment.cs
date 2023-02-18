using System;

namespace BrewLib.Util
{
    [Flags]
    public enum BoxAlignment
    {
        Centre = 0,

        Top = 1, Bottom = 2, Right = 4, Left = 8,

        TopLeft = Top | Left, TopRight = Top | Right,
        BottomLeft = Bottom | Left, BottomRight = Bottom | Right,

        Vertical = Top | Bottom, Horizontal = Left | Right
    }
}