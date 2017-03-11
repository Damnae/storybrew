using OpenTK;
using StorybrewCommon.Animations;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class PerspectiveCamera : Camera
    {
        public readonly KeyframedValue<float> PositionX = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> PositionY = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> PositionZ = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<Vector3> TargetPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> Up = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3, new Vector3(0, 1, 0));
        public readonly KeyframedValue<float> ZNear = new KeyframedValue<float>(InterpolatingFunctions.Float, 10);
        public readonly KeyframedValue<float> ZFar = new KeyframedValue<float>(InterpolatingFunctions.Float, 1000);

        public readonly KeyframedValue<float> HorizontalFov = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> VerticalFov = new KeyframedValue<float>(InterpolatingFunctions.Float);

        public override CameraState StateAt(double time)
        {
            var aspectRatio = AspectRatio;

            var cameraPosition = new Vector3(PositionX.ValueAt(time), PositionY.ValueAt(time), PositionZ.ValueAt(time));
            var targetPosition = TargetPosition.ValueAt(time);
            var up = Up.ValueAt(time).Normalized();

            float fovY;
            if (HorizontalFov.Count > 0)
            {
                var fovX = MathHelper.DegreesToRadians(HorizontalFov.ValueAt(time));
                fovY = 2 * (float)Math.Atan(Math.Tan(fovX * 0.5) / aspectRatio);
            }
            else
            {
                fovY = VerticalFov.Count > 0 ?
                    MathHelper.DegreesToRadians(VerticalFov.ValueAt(time)) :
                    2 * (float)Math.Atan(Resolution.Y * 0.5 / Math.Max(0.0001f, (cameraPosition - targetPosition).Length));
            }

            var focusDistance = Resolution.Y * 0.5 / Math.Tan(fovY * 0.5);
            var zNear = ZNear.ValueAt(time);
            var zFar = ZFar.ValueAt(time);

            var view = Matrix4.LookAt(cameraPosition, targetPosition, up);
            var projection = Matrix4.CreatePerspectiveFieldOfView(fovY,
                (float)aspectRatio, zNear, zFar);

            return new CameraState(view * projection, aspectRatio, focusDistance, ResolutionScale, zNear, zFar);
        }
    }
}
