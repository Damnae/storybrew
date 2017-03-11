using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.CommandValues;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding3d
{
    public class Object3d
    {
        public readonly KeyframedValue<CommandColor> Coloring = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor, CommandColor.White);
        public readonly KeyframedValue<float> Opacity = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        private List<Object3d> children = new List<Object3d>();

        public void Add(Object3d child)
        {
            children.Add(child);
        }

        public virtual Matrix4 WorldTransformAt(double time)
        {
            return Matrix4.Identity;
        }

        public void GenerateTreeKeyframes(double time, CameraState cameraState, Object3dState parent3dState)
        {
            var object3dState = new Object3dState(
                WorldTransformAt(time) * parent3dState.WorldTransform,
                Coloring.ValueAt(time) * parent3dState.Color,
                Opacity.ValueAt(time) * parent3dState.Opacity);

            GenerateKeyframes(time, cameraState, object3dState);
            foreach (var child in children)
                child.GenerateTreeKeyframes(time, cameraState, object3dState);
        }
        public void GenerateTreeSprite(StoryboardLayer layer)
        {
            GenerateSprite(layer);
            foreach (var child in children)
                child.GenerateTreeSprite(layer);
        }

        public virtual void GenerateKeyframes(double time, CameraState cameraState, Object3dState object3dState)
        {
        }
        public virtual void GenerateSprite(StoryboardLayer layer)
        {
        }
    }
}
