using OpenTK;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding
{
    public class IterativeSpriteState : IComparable<IterativeSpriteState>
    {
        public double Time;
        public Vector2 Position = new Vector2(320, 240);
        public Vector2 Scale = new Vector2(1, 1);
        public double Rotation = 0;
        public CommandColor Color = CommandColor.White;
        public double Opacity = 1;

        public bool IsVisible(int width, int height)
        {
            if (Opacity <= 0)
                return false;

            if (Scale.X == 0 || Scale.Y == 0)
                return false;

            var radius = Math.Max(width * Scale.X, height * Scale.Y) * 0.5 * Math.Sqrt(2);
            var bounds = OsuHitObject.WidescreenStoryboardBounds;
            if (Position.X + radius < bounds.Left || bounds.Right < Position.X - radius ||
                Position.Y + radius < bounds.Top || bounds.Bottom < Position.Y - radius)
                return false;

            return true;
        }

        public int CompareTo(IterativeSpriteState other)
        {
            return Math.Sign(Time - other.Time);
        }
    }
}
