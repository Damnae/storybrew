using OpenTK;
using StorybrewCommon.Mapset;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class CameraState
    {
        public readonly Matrix4 ViewProjection;
        public readonly double AspectRatio;
        public readonly double FocusDistance;
        public readonly double ResolutionScale;
        public readonly double ZNear;
        public readonly double ZFar;

        public CameraState(Matrix4 viewProjection, double aspectRatio, double focusDistance, double resolutionScale, double zNear, double zFar)
        {
            ViewProjection = viewProjection;
            AspectRatio = aspectRatio;
            FocusDistance = focusDistance;
            ResolutionScale = resolutionScale;
            ZNear = zNear;
            ZFar = zFar;
        }

        public Vector4 ToScreen(Matrix4 transform, Vector3 point)
        {
            var scale = new Vector2(OsuHitObject.StoryboardSize.Y * (float)AspectRatio, OsuHitObject.StoryboardSize.Y);
            var offset = (scale.X - OsuHitObject.StoryboardSize.X) * 0.5f;

            var transformedPoint = Vector4.Transform(new Vector4(point, 1), transform);
            var ndc = new Vector2(transformedPoint.X, transformedPoint.Y) / Math.Abs(transformedPoint.W);

            var screenPosition = (ndc + Vector2.One) * 0.5f * scale;
            var depth = transformedPoint.Z / transformedPoint.W;

            return new Vector4(screenPosition.X - offset, screenPosition.Y, depth, transformedPoint.W);
        }

        public double LinearizeZ(double z)
        {
            return (2 * ZNear) / (ZFar + ZNear - z * (ZFar - ZNear));
        }
    }
}
