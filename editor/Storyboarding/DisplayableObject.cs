using OpenTK;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using StorybrewCommon.Storyboarding;

namespace StorybrewEditor.Storyboarding
{
    public interface DisplayableObject
    {
        double StartTime { get; }
        double EndTime { get; }

        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, StoryboardTransform transform, Project project, FrameStats frameStats);
    }
}
