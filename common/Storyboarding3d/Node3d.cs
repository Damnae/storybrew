using OpenTK;
using StorybrewCommon.Animations;

namespace StorybrewCommon.Storyboarding3d
{
#pragma warning disable CS1591
    public class Node3d : Object3d
    {
        public readonly KeyframedValue<float> 
            PositionX = new KeyframedValue<float>(InterpolatingFunctions.Float),
            PositionY = new KeyframedValue<float>(InterpolatingFunctions.Float),
            PositionZ = new KeyframedValue<float>(InterpolatingFunctions.Float),
            ScaleX = new KeyframedValue<float>(InterpolatingFunctions.Float, 1),
            ScaleY = new KeyframedValue<float>(InterpolatingFunctions.Float, 1),
            ScaleZ = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<Quaternion> Rotation = new KeyframedValue<Quaternion>(InterpolatingFunctions.QuaternionSlerp, Quaternion.Identity);

        public override Matrix4 WorldTransformAt(double time) 
            => Matrix4.CreateScale(ScaleX.ValueAt(time), ScaleY.ValueAt(time), ScaleZ.ValueAt(time)) *
            Matrix4.CreateFromQuaternion(Rotation.ValueAt(time)) *
            Matrix4.CreateTranslation(PositionX.ValueAt(time), PositionY.ValueAt(time), PositionZ.ValueAt(time));
    }
}