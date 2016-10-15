using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class StoryboardCamera : MarshalByRefObject
    {
        public readonly KeyframedValue<Vector3> Position = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3, Vector3.Zero);
        public readonly KeyframedValue<Vector3> Direction = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3, new Vector3(0, -1, 0));
        public readonly KeyframedValue<Vector3> Up = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3, new Vector3(0, 0, 1));

        public readonly KeyframedValue<float> FieldOfView = new KeyframedValue<float>(InterpolatingFunctions.Float, 67);

        public readonly KeyframedValue<float> NearPlane = new KeyframedValue<float>(InterpolatingFunctions.Float, 0.001f);
        public readonly KeyframedValue<float> NearFade = new KeyframedValue<float>(InterpolatingFunctions.Float, 1f);
        public readonly KeyframedValue<float> FarFade = new KeyframedValue<float>(InterpolatingFunctions.Float, 999);
        public readonly KeyframedValue<float> FarPlane = new KeyframedValue<float>(InterpolatingFunctions.Float, 1000);

        public void Move(OsbEasing easing, double time, Vector3 position) => Position.Add(time, position, easing.ToEasingFunction());
        public void Move(OsbEasing easing, double time, float x, float y, float z) => Move(easing, time, new Vector3(x, y, z));
        public void Move(double time, Vector3 position) => Move(OsbEasing.None, time, position);
        public void Move(double time, float x, float y, float z) => Move(OsbEasing.None, time, x, y, z);

        public void Rotate(OsbEasing easing, double time, Vector3 direction, Vector3 up)
        {
            var easingFunction = easing.ToEasingFunction();
            Direction.Add(time, direction, easingFunction);
            Up.Add(time, up, easingFunction);
        }
        public void Rotate(double time, Vector3 direction, Vector3 up) => Rotate(OsbEasing.None, time, direction, up);

        public void LookAt(OsbEasing easing, double time, Vector3 target)
        {
            var newDirection = (target - Position.ValueAt(time)).Normalized();
            if (newDirection != Vector3.Zero)
            {
                var easingFunction = easing.ToEasingFunction();
                var direction = Direction.ValueAt(time);
                var up = Up.ValueAt(time);

                var dot = Vector3.Dot(newDirection, up);
                if (Math.Abs(dot - 1) < 0.000000001f)
                    up = -direction;
                else if (Math.Abs(dot + 1) < 0.000000001f)
                    up = direction;

                Direction.Add(time, newDirection, easingFunction);
                Up.Add(time, Vector3.Cross(Vector3.Cross(direction, up).Normalized(), direction).Normalized(), easingFunction);
            }
        }
        public void LookAt(double time, Vector3 target) => LookAt(OsbEasing.None, time, target);

        public State GetState(double time)
        {
            var viewport = OsuHitObject.StoryboardSize;
            var fovRadians = (float)Math.Min(FieldOfView.ValueAt(time) * Math.PI / 180, Math.PI - 0.0001f);
            var aspect = viewport.X / viewport.Y;

            var position = Position.ValueAt(time);
            var target = position + Direction.ValueAt(time);
            var up = Up.ValueAt(time);

            return new State(
               Matrix4.CreatePerspectiveFieldOfView(fovRadians, aspect, NearPlane.ValueAt(time), FarPlane.ValueAt(time)),
               Matrix4.LookAt(position, target, up),
               fovRadians, NearPlane.ValueAt(time), NearFade.ValueAt(time), FarFade.ValueAt(time), FarPlane.ValueAt(time));
        }

        public class State
        {
            public readonly Matrix4 Projection;
            public readonly Matrix4 View;
            public readonly Matrix4 ProjectionView;
            public readonly Quaternion Rotation;
            public readonly float FieldOfView;
            public readonly float NearPlane;
            public readonly float NearFade;
            public readonly float FarFade;
            public readonly float FarPlane;

            public State(Matrix4 projection, Matrix4 view, float fieldOfView, float nearPlane, float nearFade, float farFade, float farPlane)
            {
                Projection = projection;
                View = view;
                ProjectionView = view * projection;
                Rotation = view.ExtractRotation();
                FieldOfView = fieldOfView;
                NearPlane = nearPlane;
                NearFade = nearFade;
                FarFade = farFade;
                FarPlane = farPlane;
            }
        }
    }
}
