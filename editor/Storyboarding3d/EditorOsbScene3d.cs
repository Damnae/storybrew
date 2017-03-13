using OpenTK;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding3d;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using StorybrewEditor.Storyboarding;
using System.IO;
using System;

namespace StorybrewEditor.Storyboarding3d
{
    public class EditorOsbScene3d : OsbScene3d, DisplayableObject
    {
        public StoryboardCamera camera = new StoryboardCamera();
        public override StoryboardCamera Camera => camera;

        public EditorOsbContainer3d rootContainer = new EditorOsbContainer3d();
        public override OsbContainer3d RootContainer => rootContainer;

        public override double StartTime => 0;
        public override double EndTime => 0;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project)
        {
            var rootState = new StoryboardObject3d.State3d(Matrix4.Identity, CommandColor.White, opacity);
            var cameraState = this.camera.GetState(project.DisplayTime * 1000);

            rootContainer.Draw(drawContext, camera, bounds, project, cameraState, rootState);
        }

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
        }
    }
}
