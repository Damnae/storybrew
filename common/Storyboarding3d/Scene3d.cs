#if DEBUG
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Commands;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class Scene3d
    {
        public readonly Node3d Root = new Node3d();

        public void Add(Object3d child)
        {
            Root.Add(child);
        }

        public void Generate(Camera camera, StoryboardLayer defaultLayer, double startTime, double endTime, double timeStep)
        {
            Root.GenerateTreeSprite(defaultLayer);
            for (var time = startTime; time < endTime + 5; time += timeStep)
                Root.GenerateTreeStates(time, camera);
            Root.GenerateTreeCommands();
        }

        public void Generate(Camera camera, StoryboardLayer defaultLayer, double startTime, double endTime, Beatmap beatmap, int divisor = 4)
        {
            Root.GenerateTreeSprite(defaultLayer);
            beatmap.ForEachTick((int)startTime, (int)endTime, divisor, (timingPoint, time, beatCount, tickCount) =>
                Root.GenerateTreeStates(time, camera));
            Root.GenerateTreeCommands();
        }

        public void Generate(Camera camera, StoryboardLayer defaultLayer, double startTime, double endTime, double timeStep, int loopCount, Action<LoopCommand, OsbSprite> action = null)
        {
            Root.GenerateTreeSprite(defaultLayer);
            for (var time = startTime; time < endTime + 5; time += timeStep)
                Root.GenerateTreeStates(time, camera);
            Root.GenerateTreeLoopCommands(startTime, endTime, loopCount, action, offsetCommands: true);
        }
    }
}
#endif