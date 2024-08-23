#if DEBUG
using OpenTK;
using StorybrewCommon.Animations;

namespace StorybrewCommon.Storyboarding3d
{
    public class Node3d : Object3d
    {
        public readonly KeyframedValue<float> PositionX = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> PositionY = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> PositionZ = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> ScaleX = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<float> ScaleY = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<float> ScaleZ = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<Quaternion> Rotation = new KeyframedValue<Quaternion>(InterpolatingFunctions.QuaternionSlerp, Quaternion.Identity);

        public override Matrix4 WorldTransformAt(double time)
        {
            return Matrix4.CreateScale(ScaleX.ValueAt(time), ScaleY.ValueAt(time), ScaleZ.ValueAt(time))
                * Matrix4.CreateFromQuaternion(Rotation.ValueAt(time))
                * Matrix4.CreateTranslation(PositionX.ValueAt(time), PositionY.ValueAt(time), PositionZ.ValueAt(time));
        }
    }
}
#endif