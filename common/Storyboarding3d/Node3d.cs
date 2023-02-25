using OpenTK;
using StorybrewCommon.Animations;

namespace StorybrewCommon.Storyboarding3d
{
    ///<summary> Represents a node in the three-dimensional world space. </summary>
    public class Node3d : Object3d
    {
        ///<summary> Represents the node's X-position in the 3D world. </summary>
        public readonly KeyframedValue<float> PositionX = new KeyframedValue<float>(InterpolatingFunctions.Float);

        ///<summary> Represents the node's Y-position in the 3D world. </summary>
        public readonly KeyframedValue<float> PositionY = new KeyframedValue<float>(InterpolatingFunctions.Float);

        ///<summary> Represents the node's Z-position in the 3D world. </summary>
        public readonly KeyframedValue<float> PositionZ = new KeyframedValue<float>(InterpolatingFunctions.Float);

        ///<summary> Represents the node's relative X-scale of children objects. </summary>
        public readonly KeyframedValue<float> ScaleX = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        ///<summary> Represents the node's relative Y-scale of children objects. </summary>
        public readonly KeyframedValue<float> ScaleY = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        ///<summary> Represents the node's relative Z-scale of children objects. </summary>
        public readonly KeyframedValue<float> ScaleZ = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        ///<summary> Represents the node's quaternion rotation about the origin. </summary>
        public readonly KeyframedValue<Quaternion> Rotation = new KeyframedValue<Quaternion>(InterpolatingFunctions.QuaternionSlerp, Quaternion.Identity);

        ///<inheritdoc/>
        public override Matrix4 WorldTransformAt(double time) 
            => Matrix4.CreateScale(ScaleX.ValueAt(time), ScaleY.ValueAt(time), ScaleZ.ValueAt(time)) *
            Matrix4.CreateFromQuaternion(Rotation.ValueAt(time)) *
            Matrix4.CreateTranslation(PositionX.ValueAt(time), PositionY.ValueAt(time), PositionZ.ValueAt(time));
    }
}