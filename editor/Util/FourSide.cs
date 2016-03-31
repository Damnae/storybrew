using OpenTK;
using StorybrewEditor.UserInterface;

namespace StorybrewEditor.Util
{
    public struct FourSide
    {
        public static readonly FourSide Zero = new FourSide(0);

        public readonly float Top;
        public readonly float Right;
        public readonly float Bottom;
        public readonly float Left;

        public float Horizontal => Left + Right;
        public float Vertical => Top + Bottom;

        public FourSide(float all)
        {
            Top = Right = Bottom = Left = all;
        }

        public FourSide(float vertical, float horizontal)
        {
            Top = Bottom = vertical;
            Right = Left = horizontal;
        }

        public FourSide(float top, float horizontal, float bottom)
        {
            Top = top;
            Right = Left = horizontal;
            Bottom = bottom;
        }

        public FourSide(float top, float right, float bottom, float left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public static bool operator ==(FourSide left, FourSide right)
            => left.Top == right.Top && left.Right == right.Right && left.Bottom == right.Bottom && left.Left == right.Left;

        public static bool operator !=(FourSide left, FourSide right)
            => !(left == right);

        public float GetHorizontalOffset(UiAlignment alignment)
           => alignment.HasFlag(UiAlignment.Left) ? Left : alignment.HasFlag(UiAlignment.Right) ? -Right : 0;

        public float GetVerticalOffset(UiAlignment alignment)
           => alignment.HasFlag(UiAlignment.Top) ? Top : alignment.HasFlag(UiAlignment.Bottom) ? -Bottom : 0;

        public Vector2 GetOffset(UiAlignment alignment)
           => new Vector2(GetHorizontalOffset(alignment), GetVerticalOffset(alignment));

        public override string ToString()
        {
            return $"{Top}, {Right}, {Bottom}, {Left}";
        }
    }

    public struct FourSide<T> where T : class
    {
        public readonly T Top;
        public readonly T Right;
        public readonly T Bottom;
        public readonly T Left;

        public FourSide(T all)
        {
            Top = Right = Bottom = Left = all;
        }

        public FourSide(T vertical, T horizontal)
        {
            Top = Bottom = vertical;
            Right = Left = horizontal;
        }

        public FourSide(T top, T horizontal, T bottom)
        {
            Top = top;
            Right = Left = horizontal;
            Bottom = bottom;
        }

        public FourSide(T top, T right, T bottom, T left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public static bool operator ==(FourSide<T> left, FourSide<T> right)
            => left.Top == right.Top && left.Right == right.Right && left.Bottom == right.Bottom && left.Left == right.Left;

        public static bool operator !=(FourSide<T> left, FourSide<T> right)
            => !(left == right);

        public override string ToString()
        {
            return $"{Top}, {Right}, {Bottom}, {Left}";
        }
    }
}
