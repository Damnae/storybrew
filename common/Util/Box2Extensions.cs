using OpenTK;
using System;

namespace StorybrewCommon.Util
{
    public static class Box2Extensions
    {
        /// <summary>
        /// Assumes that the Box2 intersect.
        /// </summary>
        public static Box2 IntersectWith(this Box2 box2, Box2 other)
            => new Box2(Math.Max(box2.Left, other.Left), Math.Max(box2.Top, other.Top), 
                Math.Min(box2.Right, other.Right), Math.Min(box2.Bottom, other.Bottom));
    }
}
