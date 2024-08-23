﻿#if DEBUG
using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorybrewCommon.Storyboarding3d
{
    public class Object3d
    {
        private readonly List<Object3d> children = new List<Object3d>();

        public readonly KeyframedValue<CommandColor> Coloring = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor, CommandColor.White);
        public readonly KeyframedValue<float> Opacity = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public StoryboardSegment Segment;

        public bool DrawBelowParent = false;

        public bool InheritsColor = true;
        public bool InheritsOpacity = true;
        public bool ChildrenInheritLayer = true;

        public virtual IEnumerable<CommandGenerator> CommandGenerators { get { yield break; } }

        public void Add(Object3d child)
        {
            children.Add(child);
        }

        public virtual Matrix4 WorldTransformAt(double time)
        {
            return Matrix4.Identity;
        }

        public void GenerateTreeSprite(StoryboardSegment parentSegment)
        {
            var layer = Segment ?? parentSegment;
            var childrenLayer = ChildrenInheritLayer ? layer : parentSegment;

            foreach (var child in children.Where(c => c.DrawBelowParent))
                child.GenerateTreeSprite(childrenLayer);

            GenerateSprite(layer);

            foreach (var child in children.Where(c => !c.DrawBelowParent))
                child.GenerateTreeSprite(childrenLayer);
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
        public void GenerateTreeCommands(Action<Action, OsbSprite> action = null, double? startTime = null, double? endTime = null, double timeOffset = 0, bool loopable = false)
        {
            GenerateCommands(action, startTime, endTime, timeOffset, loopable);
            foreach (var child in children)
                child.GenerateTreeCommands(action, startTime, endTime, timeOffset, loopable);
        }

        public void GenerateTreeLoopCommands(double startTime, double endTime, int loopCount, Action<LoopCommand, OsbSprite> action = null, bool offsetCommands = true)
        {
            GenerateTreeCommands((createCommands, s) =>
            {
                var loop = s.StartLoopGroup(startTime, loopCount);
                createCommands();
                action?.Invoke(loop, s);
                s.EndGroup();
            }, startTime, endTime, offsetCommands ? -startTime : 0, true);
        }

        public void DoTree(Action<Object3d> action)
        {
            action(this);
            foreach (var child in children)
                child.DoTree(action);
        }
        public void DoTreeSprite(Action<OsbSprite> action)
        {
            var sprites = (this as HasOsbSprites)?.Sprites;
            if (sprites != null)
                foreach (var sprite in sprites)
                    action(sprite);
            foreach (var child in children)
                child.DoTreeSprite(action);
        }

        public virtual void GenerateSprite(StoryboardSegment parentSegment)
        {
        }
        public virtual void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
        }
        public virtual void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable)
        {
        }

        public void ConfigureGenerators(Action<CommandGenerator> action)
        {
            foreach (var generator in CommandGenerators)
                action(generator);
        }
    }
}
#endif