using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Commands;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    ///<summary> Represents a 3D scene with a camera and root. </summary>
    public class Scene3d
    {
        ///<summary> Represents the scene's base node or root. </summary>
        public readonly Node3d Root = new Node3d();

        ///<summary> Adds a 3D object to the scene's root. </summary>
        public void Add(Object3d child) => Root.Add(child);

        /// <summary> 
        /// Generates a 3D scene from <paramref name="startTime"/> to <paramref name="endTime"/> with given iteration period <paramref name="timeStep"/>.
        /// </summary>
        public void Generate(Camera camera, StoryboardSegment segment, double startTime, double endTime, double timeStep)
        {
            Root.GenerateTreeSprite(segment);
            for (double time = startTime; time < endTime + 5; time += timeStep) Root.GenerateTreeStates(time, camera);
            Root.GenerateTreeCommands();
        }

        /// <summary> 
        /// Generates a 3D scene from <paramref name="startTime"/> to <paramref name="endTime"/> with an iteration period based on the beatmap's timing point and <paramref name="divisor"/>.
        /// </summary>
        public void Generate(Camera camera, StoryboardSegment segment, double startTime, double endTime, Beatmap beatmap, int divisor = 4)
        {
            Root.GenerateTreeSprite(segment);
            beatmap.ForEachTick((int)startTime, (int)endTime, divisor, (t, time, b, tC) => Root.GenerateTreeStates(time, camera));
            Root.GenerateTreeCommands();
        }
        
        /// <summary> 
        /// Generates a looping 3D scene from <paramref name="startTime"/> to <paramref name="endTime"/> with given iteration period <paramref name="timeStep"/> and loop count <paramref name="loopCount"/>.
        /// </summary>
        public void Generate(Camera camera, StoryboardSegment segment, double startTime, double endTime, double timeStep, int loopCount, Action<LoopCommand, OsbSprite> action = null)
        {
            Root.GenerateTreeSprite(segment);
            for (var time = startTime; time < endTime + 5; time += timeStep) Root.GenerateTreeStates(time, camera);
            Root.GenerateTreeLoopCommands(startTime, endTime, loopCount, action, offsetCommands: true);
        }
    }
}