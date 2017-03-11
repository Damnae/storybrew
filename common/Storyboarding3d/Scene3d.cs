using OpenTK;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding3d
{
    public class Scene3d
    {
        public readonly Node3d Root = new Node3d();

        public void Add(Object3d child)
        {
            Root.Add(child);
        }

        public void Generate(Camera camera, StoryboardLayer layer, double startTime, double endTime, double timeStep)
        {
            for (var time = startTime; time < endTime; time += timeStep)
            {
                var cameraState = camera.StateAt(time);
                var object3dState = new Object3dState(Matrix4.Identity, CommandColor.White, 1);
                Root.GenerateTreeKeyframes(time, cameraState, object3dState);
            }
            Root.GenerateTreeSprite(layer, startTime, endTime);
        }
    }
}
