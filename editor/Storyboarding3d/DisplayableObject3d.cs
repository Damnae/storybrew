using OpenTK;
using StorybrewCommon.Storyboarding3d;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using StorybrewEditor.Storyboarding;

namespace StorybrewEditor.Storyboarding3d
{
    public interface DisplayableObject3d
    {
        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, Project project, StoryboardCamera.State cameraState, StoryboardObject3d.State3d parentState = null);
    }
}
