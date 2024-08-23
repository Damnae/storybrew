#if DEBUG
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
        public readonly double NearClip;
        public readonly double NearFade;
        public readonly double FarFade;
        public readonly double FarClip;

        public CameraState(Matrix4 viewProjection, double aspectRatio, double focusDistance, double resolutionScale, double nearClip, double nearFade, double farFade, double farClip)
        {
            ViewProjection = viewProjection;
            AspectRatio = aspectRatio;
            FocusDistance = focusDistance;
            ResolutionScale = resolutionScale;
            NearClip = nearClip;
            NearFade = nearFade;
            FarFade = farFade;
            FarClip = farClip;
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
            return (2 * NearClip) / (FarClip + NearClip - z * (FarClip - NearClip));
        }

        public float OpacityAt(float distance)
        {
            if (distance < NearFade)
                return (float)Math.Max(0, Math.Min((distance - NearClip) / (NearFade - NearClip), 1));
            else if (distance > FarFade)
                return (float)Math.Max(0, Math.Min((FarClip - distance) / (FarClip - FarFade), 1));
            return 1;
        }
    }
}
#endif