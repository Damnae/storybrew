using OpenTK;
using StorybrewCommon.Util;
using System;

namespace StorybrewCommon.Storyboarding
{
    public class StoryboardTransform
    {
        private readonly Affine2 transform;
        private readonly float transformScale;
        private readonly double transformAngle;

        public StoryboardTransform(StoryboardTransform parent, Vector2 origin, Vector2 position, double rotation, float scale)
        {
            transform = parent?.transform ?? Affine2.Identity;
            if (position != Vector2.Zero)
                transform.Translate(position.X, position.Y);
            if (rotation != 0)
                transform.Rotate((float)rotation);
            if (scale != 1)
                transform.Scale(scale, scale);
            if (origin != Vector2.Zero)
                transform.Translate(-origin.X, -origin.Y);

            transformScale = (parent?.transformScale ?? 1) * scale;

            // https://math.stackexchange.com/questions/13150/extracting-rotation-scale-values-from-2d-transformation-matrix/13165#13165
            transformAngle = Math.Atan2(-transform.M21, transform.M11); // OR Math.Atan2(-transform.M22, transform.M12);
        }

        public Vector2 ApplyToPosition(Vector2 value)
            => transform.Transform(value);

        public Vector2 ApplyToPositionXY(Vector2 value)
            => transform.TransformSeparate(value);

        public float ApplyToPositionX(float value)
            => transform.TransformX(value);

        public float ApplyToPositionY(float value)
            => transform.TransformY(value);

        public float ApplyToRotation(float value)
            => value + (float)transformAngle;

        public float ApplyToScale(float value)
            => value * transformScale;

        public Vector2 ApplyToScale(Vector2 value)
            => value * transformScale;
    }
}
