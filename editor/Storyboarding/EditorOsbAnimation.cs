using OpenTK;
using StorybrewCommon.Storyboarding;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;

namespace StorybrewEditor.Storyboarding
{
    public class EditorOsbAnimation : OsbAnimation, DisplayableObject, HasPostProcess
    {
        public string ScriptName { get; set; }
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, StoryboardTransform transform, Project project, FrameStats frameStats)
            => EditorOsbSprite.Draw(drawContext, camera, bounds, opacity, transform, project, frameStats, this);

        public void PostProcess()
        {
            if (InGroup) 
                EndGroup();
        }
    }
}
