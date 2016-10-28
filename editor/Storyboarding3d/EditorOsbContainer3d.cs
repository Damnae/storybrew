using OpenTK;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding3d;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using StorybrewEditor.Storyboarding;
using System.Collections.Generic;

namespace StorybrewEditor.Storyboarding3d
{
    public class EditorOsbContainer3d : OsbContainer3d, DisplayableObject3d
    {
        public List<StoryboardObject3d> children = new List<StoryboardObject3d>();
        public List<DisplayableObject3d> drawableChildren = new List<DisplayableObject3d>();

        public override OsbContainer3d CreateContainer3d()
        {
            var child = new EditorOsbContainer3d();
            children.Add(child);
            drawableChildren.Add(child);
            return child;
        }

        public override OsbSprite3d CreateSprite3d(string path, OsbOrigin origin = OsbOrigin.Centre)
        {
            var child = new EditorOsbSprite3d()
            {
                TexturePath = path,
                Origin = origin,
            };
            children.Add(child);
            drawableChildren.Add(child);
            return child;
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, Project project, StoryboardCamera.State cameraState, State3d parentState)
        {
            var state = GetState3d(project.DisplayTime * 1000, parentState);
            foreach (var child in drawableChildren)
                child.Draw(drawContext, camera, bounds, project, cameraState, state);
        }
    }
}
