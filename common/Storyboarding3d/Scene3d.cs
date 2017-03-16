using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;

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
            Root.GenerateTreeSprite(layer);
            for (var time = startTime; time < endTime + 0.005; time += timeStep)
                Root.GenerateTreeKeyframes(time, camera.StateAt(time), Object3dState.InitialState);
            Root.GenerateTreeCommands();
        }

        public void Generate(Camera camera, StoryboardLayer layer, double startTime, double endTime, Beatmap beatmap, int divisor = 4)
        {
            Root.GenerateTreeSprite(layer);
            Beatmap.ForEachTick(beatmap, (int)startTime, (int)endTime, divisor, (timingPoint, time, beatCount, tickCount) =>
                Root.GenerateTreeKeyframes(time, camera.StateAt(time), Object3dState.InitialState));
            Root.GenerateTreeCommands();
        }
    }
}
