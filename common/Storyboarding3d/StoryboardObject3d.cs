using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public abstract class StoryboardObject3d : MarshalByRefObject
    {
        public readonly KeyframedValue<float> PositionX = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> PositionY = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<float> PositionZ = new KeyframedValue<float>(InterpolatingFunctions.Float);
        public readonly KeyframedValue<Quaternion> Rotation = new KeyframedValue<Quaternion>(InterpolatingFunctions.QuaternionSlerp, Quaternion.Identity);
        public readonly KeyframedValue<Vector3> Scaling = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3, Vector3.One);
        public readonly KeyframedValue<CommandColor> Coloring = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor, CommandColor.White);
        public readonly KeyframedValue<float> Opacity = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        public void Move(OsbEasing easing, double time, float x, float y, float z)
        {
            var easingFunction = easing.ToEasingFunction();
            PositionX.Add(time, x, easingFunction);
            PositionY.Add(time, y, easingFunction);
            PositionZ.Add(time, z, easingFunction);
        }
        public void Move(OsbEasing easing, double time, Vector3 position) => Move(easing, time, position.X, position.Y, position.Z);
        public void Move(double time, float x, float y, float z) => Move(OsbEasing.None, time, x, y, z);
        public void Move(double time, Vector3 position) => Move(OsbEasing.None, time, position);

        public void MoveXY(OsbEasing easing, double time, float x, float y)
        {
            var easingFunction = easing.ToEasingFunction();
            PositionX.Add(time, x, easingFunction);
            PositionY.Add(time, y, easingFunction);
        }
        public void MoveXY(OsbEasing easing, double time, Vector2 position) => MoveXY(easing, time, position.X, position.Y);
        public void MoveXY(double time, float x, float y) => MoveXY(OsbEasing.None, time, x, y);
        public void MoveXY(double time, Vector2 position) => MoveXY(OsbEasing.None, time, position);

        public void MoveX(OsbEasing easing, double time, float x) => PositionX.Add(time, x, easing.ToEasingFunction());
        public void MoveX(double time, float x) => MoveX(OsbEasing.None, time, x);

        public void MoveY(OsbEasing easing, double time, float y) => PositionY.Add(time, y, easing.ToEasingFunction());
        public void MoveY(double time, float y) => MoveY(OsbEasing.None, time, y);

        public void MoveZ(OsbEasing easing, double time, float z) => PositionZ.Add(time, z, easing.ToEasingFunction());
        public void MoveZ(double time, float z) => MoveZ(OsbEasing.None, time, z);

        public void Rotate(OsbEasing easing, double time, Quaternion rotation) => Rotation.Add(time, rotation, easing.ToEasingFunction());
        public void Rotate(OsbEasing easing, double time, float pitch, float yaw, float roll) => Rotate(easing, time, Quaternion.FromEulerAngles(pitch, yaw, roll));
        public void Rotate(double time, Quaternion rotation) => Rotate(OsbEasing.None, time, rotation);
        public void Rotate(double time, float pitch, float yaw, float roll) => Rotate(OsbEasing.None, time, pitch, yaw, roll);

        public void Scale(OsbEasing easing, double time, Vector3 scale) => Scaling.Add(time, scale, easing.ToEasingFunction());
        public void Scale(double time, Vector3 scale) => Scale(OsbEasing.None, time, scale);

        public void Color(OsbEasing easing, double time, CommandColor color) => Coloring.Add(time, color, easing.ToEasingFunction());
        public void Color(OsbEasing easing, double time, double r, double g, double b) => Color(easing, time, new CommandColor(r, g, b));
        public void Color(double time, CommandColor color) => Color(OsbEasing.None, time, color);
        public void Color(double time, double r, double g, double b) => Color(OsbEasing.None, time, r, g, b);

        public void ColorHsb(OsbEasing easing, double time, double hue, double saturation, double brightness) => Color(easing, time, CommandColor.FromHsb(hue, saturation, brightness));
        public void ColorHsb(double time, double hue, double saturation, double brightness) => ColorHsb(OsbEasing.None, time, hue, saturation, brightness);

        public void Fade(OsbEasing easing, double time, float opacity) => Opacity.Add(time, opacity, easing.ToEasingFunction());
        public void Fade(double time, float opacity) => Fade(OsbEasing.None, time, opacity);

        public State3d GetState3d(double time, State3d parentState)
        {
            var transform =
                Matrix4.CreateScale(Scaling.ValueAt(time)) *
                Matrix4.CreateFromQuaternion(Rotation.ValueAt(time)) *
                Matrix4.CreateTranslation(PositionX.ValueAt(time), PositionY.ValueAt(time), PositionZ.ValueAt(time)) *
                parentState.Transform;
            return new State3d(transform, parentState.Color * Coloring.ValueAt(time), parentState.Opacity * Opacity.ValueAt(time));
        }

        public State2d GetState2d(State3d state3d, StoryboardCamera.State cameraState)
        {
            var position = Vector4.Transform(new Vector4(state3d.Position, 1), cameraState.ProjectionView);
            var rotation = (float)calculateRoll(state3d.Rotation * cameraState.Rotation.Inverted());

            var focalLength = 1.0 / Math.Tan(cameraState.FieldOfView * 0.5);
            var nearPlaneWidth = OsuHitObject.StoryboardSize.X * focalLength * 0.5;
            var scale = state3d.Scale.Xy * (float)nearPlaneWidth / position.W;

            var opacity = state3d.Opacity;
            if (position.W < cameraState.NearFade)
                opacity *= (position.W - cameraState.NearPlane) / (cameraState.NearFade - cameraState.NearPlane);
            else if (position.W > cameraState.FarFade)
                opacity *= (cameraState.FarPlane - position.W) / (cameraState.FarPlane - cameraState.FarFade);
            opacity = Math.Max(0, Math.Min(opacity, 1));

            return new State2d(position.Xy + OsuHitObject.StoryboardSize * 0.5f, position.Z, rotation, scale, opacity);
        }

        private double calculateRoll(Quaternion rotation)
        {
            var x = rotation.X;
            var y = rotation.Y;
            var z = rotation.Z;
            var w = rotation.W;
            return -Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z));
        }

        public class State3d
        {
            public readonly Matrix4 Transform;
            public Vector3 Position => Transform.ExtractTranslation();
            public Quaternion Rotation => Transform.ExtractRotation();
            public Vector3 Scale => Transform.ExtractScale();
            public readonly CommandColor Color;
            public readonly float Opacity;

            public State3d(Matrix4 transform, CommandColor color, float opacity)
            {
                Transform = transform;
                Color = color;
                Opacity = opacity;
            }
        }

        public class State2d
        {
            public readonly Vector2 Position;
            public readonly float Depth;
            public readonly float Rotation;
            public readonly Vector2 Scale;
            public readonly float Opacity;

            public State2d(Vector2 position, float depth, float rotation, Vector2 scale, float opacity)
            {
                Position = position;
                Depth = depth;
                Rotation = rotation;
                Scale = scale;
                Opacity = opacity;
            }
        }
    }
}
