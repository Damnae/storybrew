#if DEBUG
using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding3d
{
    public class Object3d
    {
        private List<Object3d> children = new List<Object3d>();

        public readonly KeyframedValue<CommandColor> Coloring = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor, CommandColor.White);
        public readonly KeyframedValue<float> Opacity = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public StoryboardLayer Layer;

        public bool InheritsColor = true;
        public bool InheritsOpacity = true;
        public bool InheritsLayer = true;

        public void Add(Object3d child)
        {
            children.Add(child);
        }

        public virtual Matrix4 WorldTransformAt(double time)
        {
            return Matrix4.Identity;
        }

        public void GenerateTreeSprite(StoryboardLayer defaultLayer)
        {
            var layer = Layer ?? defaultLayer;
            GenerateSprite(layer);
            foreach (var child in children)
                child.GenerateTreeSprite(InheritsLayer ? layer : defaultLayer);
        }
        public void GenerateTreeStates(double time, Camera camera, Object3dState parent3dState = null)
            => GenerateTreeStates(time, camera.StateAt(time), parent3dState);
        public void GenerateTreeStates(double time, CameraState cameraState, Object3dState parent3dState = null)
        {
            parent3dState = parent3dState ?? Object3dState.InitialState;

            var object3dState = new Object3dState(
                WorldTransformAt(time) * parent3dState.WorldTransform,
                Coloring.ValueAt(time) * (InheritsColor ? parent3dState.Color : CommandColor.White),
                Opacity.ValueAt(time) * (InheritsOpacity ? parent3dState.Opacity : 1));

            GenerateStates(time, cameraState, object3dState);
            foreach (var child in children)
                child.GenerateTreeStates(time, cameraState, object3dState);
        }
        public void GenerateTreeCommands(Action<Action, OsbSprite> action = null, double timeOffset = 0)
        {
            GenerateCommands(action, timeOffset);
            foreach (var child in children)
                child.GenerateTreeCommands(action, timeOffset);
        }

        public void GenerateTreeLoopCommands(double startTime, int loopCount, Action<LoopCommand, OsbSprite> action = null, bool offsetCommands = true)
        {
            GenerateTreeCommands((createCommands, s) =>
            {
                var loop = s.StartLoopGroup(startTime, loopCount);
                createCommands();
                action?.Invoke(loop, s);
                s.EndGroup();
            }, offsetCommands ? -startTime : 0);
        }

        public void DoTree(Action<Object3d> action)
        {
            action(this);
            foreach (var child in children)
                child.DoTree(action);
        }
        public void DoTreeSprite(Action<OsbSprite> action)
        {
            var sprite = (this as HasOsbSprite)?.Sprite;
            if (sprite != null)
                action(sprite);
            foreach (var child in children)
                child.DoTreeSprite(action);
        }

        public virtual void GenerateSprite(StoryboardLayer layer)
        {
        }
        public virtual void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
        }
        public virtual void GenerateCommands(Action<Action, OsbSprite> action, double timeOffset)
        {
        }
    }
}
#endif