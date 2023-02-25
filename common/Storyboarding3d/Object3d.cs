using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StorybrewCommon.Storyboarding3d
{
#pragma warning disable CS1591
    public class Object3d
    {
        readonly List<Object3d> children = new List<Object3d>();

        ///<summary> A keyframed value representing this instance's color keyframes. </summary>
        public readonly KeyframedValue<CommandColor> Coloring = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor, CommandColor.White);

        ///<summary> A keyframed value representing this instance's opacity/fade keyframes. </summary>
        public readonly KeyframedValue<float> Opacity = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        ///<summary> Represents the instance's segment. </summary>
        public StoryboardSegment Segment;

        public bool InheritsColor = true, InheritsOpacity = true, DrawBelowParent = false, ChildrenInheritLayer = true;

        ///<summary> Gets this instance's <see cref="CommandGenerator"/>s. </summary>
        public virtual IEnumerable<CommandGenerator> CommandGenerators { get { yield break; } }

        ///<summary> Adds a 3D sub-object to this instance. </summary>
        public void Add(Object3d child) => children.Add(child);

        ///<summary> Gets this instance's 3D-world transform at <paramref name="time"/>. </summary>
        public virtual Matrix4 WorldTransformAt(double time) => Matrix4.Identity;

        public void GenerateTreeSprite(StoryboardSegment parentSegment)
        {
            var layer = Segment ?? parentSegment;
            var childrenLayer = ChildrenInheritLayer ? layer : parentSegment;

            foreach (var child in children.Where(c => c.DrawBelowParent)) child.GenerateTreeSprite(childrenLayer);
            GenerateSprite(layer);
            foreach (var child in children.Where(c => !c.DrawBelowParent)) child.GenerateTreeSprite(childrenLayer);
        }

        public void GenerateTreeStates(double time, Camera camera)
            => GenerateTreeStates(time, camera.StateAt(time), Object3dState.InitialState);

        public void GenerateTreeStates(double time, CameraState cameraState, Object3dState parent3dState)
        {
            var object3dState = new Object3dState(
                WorldTransformAt(time) * parent3dState.WorldTransform,
                Coloring.ValueAt(time) * (InheritsColor ? parent3dState.Color : CommandColor.White),
                Opacity.ValueAt(time) * (InheritsOpacity ? parent3dState.Opacity : 1));

            GenerateStates(time, cameraState, object3dState);
            for (var i = 0; i < children.Count; i++) children[i].GenerateTreeStates(time, cameraState, object3dState);
        }
        public void GenerateTreeCommands(Action<Action, OsbSprite> action = null, double? startTime = null, double? endTime = null, double timeOffset = 0, bool loopable = false)
        {
            GenerateCommands(action, startTime, endTime, timeOffset, loopable);
            for (var i = 0; i < children.Count; i++) children[i].GenerateTreeCommands(action, startTime, endTime, timeOffset, loopable);
        }
        public void GenerateTreeLoopCommands(double startTime, double endTime, int loopCount, Action<LoopCommand, OsbSprite> action = null, bool offsetCommands = true)
            => GenerateTreeCommands((createCommands, s) =>
        {
            var loop = s.StartLoopGroup(startTime, loopCount);
            createCommands();
            action?.Invoke(loop, s);
            s.EndGroup();
        }, startTime, endTime, offsetCommands ? -startTime : 0, true);
        public void DoTree(Action<Object3d> action)
        {
            action(this);
            for (var i = 0; i < children.Count; i++) children[i].DoTree(action);
        }
        public void DoTreeSprite(Action<OsbSprite> action)
        {
            var sprites = (this as HasOsbSprites)?.Sprites;
            if (sprites != null) foreach (var sprite in sprites) action(sprite);
            for (var i = 0; i < children.Count; i++) children[i].DoTreeSprite(action);
        }

        public virtual void GenerateSprite(StoryboardSegment parentSegment) { }
        public virtual void GenerateStates(double time, CameraState cameraState, Object3dState object3dState) { }
        public virtual void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable) { }

        public void ConfigureGenerators(Action<CommandGenerator> action) => Parallel.ForEach(CommandGenerators, generator => action(generator));
    }
    public struct Object3dState
    {
        public static readonly Object3dState InitialState = new Object3dState(Matrix4.Identity, CommandColor.White, 1);
        public readonly Matrix4 WorldTransform;
        public readonly CommandColor Color;
        public readonly float Opacity;

        public Object3dState(Matrix4 worldTransform, CommandColor color, float opacity)
        {
            WorldTransform = worldTransform;
            Color = color;
            Opacity = opacity;
        }
    }
}