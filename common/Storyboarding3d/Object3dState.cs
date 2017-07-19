#if DEBUG
using OpenTK;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding3d
{
    public class Object3dState
    {
        public static readonly Object3dState InitialState = new Object3dState(Matrix4.Identity, CommandColor.White, 1);

        public readonly Matrix4 WorldTransform;
        public readonly CommandColor Color;
        public readonly float Opacity;

        public Object3dState(Matrix4 worldTransform, CommandColor color, float opacity)
        {
            WorldTransform = worldTransform;
            Color = color;
            Opacity = opacity;
        }
    }
}
#endif