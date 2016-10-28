using OpenTK;
using StorybrewCommon.Storyboarding;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;

namespace StorybrewEditor.Storyboarding
{
    public class EditorOsbAnimation : OsbAnimation, DisplayableObject
    {
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project)
            => EditorOsbSprite.Draw(drawContext, camera, bounds, opacity, project, this);
    }
}
