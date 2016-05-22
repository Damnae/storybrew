using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;

namespace StorybrewEditor.Storyboarding
{
    public interface DisplayableObject
    {
        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project);
    }
}
