using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using OpenTK;

namespace StorybrewEditor.Storyboarding
{
    public interface DisplayableObject
    {
        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project, FrameStats frameStats);
    }
}
