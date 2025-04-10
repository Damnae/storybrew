using OpenTK;
using StorybrewCommon.Util;
using System;
using System.Diagnostics;

namespace StorybrewCommon.Storyboarding
{
    public class StoryboardTransform
    {
        public readonly static StoryboardTransform Identity = new StoryboardTransform(null, Vector2.Zero, Vector2.Zero, 0, 1, Vector2.Zero, 0, 1);

        private readonly Affine2 transform;
        private readonly Affine2 inverseTransform;
        private readonly float transformScale;
        private readonly double transformAngle;

        public StoryboardTransform(StoryboardTransform parent,
            Vector2 origin, Vector2 position, double rotation, float scale,
            Vector2 placementPosition, double placementRotation, float placementScale)
        {
            transform = parent?.transform ?? Affine2.Identity;
            inverseTransform = parent?.inverseTransform ?? Affine2.Identity;
            if (position != Vector2.Zero)
            {
                transform.Translate(position.X, position.Y);
                inverseTransform.TranslateInverse(-position.X, -position.Y);
            }
            if (rotation != 0)
            {
                transform.Rotate((float)rotation);
                inverseTransform.RotateInverse(-(float)rotation);
            }
            if (scale != 1)
            {
                transform.Scale(scale, scale);
                inverseTransform.ScaleInverse(1 / scale, 1 / scale);
            }

            if (placementPosition != Vector2.Zero)
            {
                transform.Translate(placementPosition.X, placementPosition.Y);
                inverseTransform.TranslateInverse(-placementPosition.X, -placementPosition.Y);
            }
            if (placementRotation != 0)
            {
                transform.Rotate((float)placementRotation);
                inverseTransform.RotateInverse(-(float)placementRotation);
            }
            if (placementScale != 1)
            {
                transform.Scale(placementScale, placementScale);
                inverseTransform.ScaleInverse(1 / placementScale, 1 / placementScale);
            }

            if (origin != Vector2.Zero)
            {
                transform.Translate(-origin.X, -origin.Y);
                inverseTransform.TranslateInverse(origin.X, origin.Y);
            }

            transformScale = (parent?.transformScale ?? 1) * scale * placementScale;

            // https://math.stackexchange.com/questions/13150/extracting-rotation-scale-values-from-2d-transformation-matrix/13165#13165
            transformAngle = Math.Atan2(-transform.M21, transform.M11); // OR Math.Atan2(-transform.M22, transform.M12);
        }

        public Vector2 ApplyToPosition(Vector2 value)
            => transform.Transform(value);

        public Vector2 ApplyToPositionInverse(Vector2 value)
            => inverseTransform.Transform(value);

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
