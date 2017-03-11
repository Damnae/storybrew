using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class Line3d : Object3d
    {
        public string SpritePath;
        public bool Additive;

        public float Thickness = 1;
        public readonly KeyframedValue<Vector3> StartPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> EndPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);

        private readonly CommandGenerator generator = new CommandGenerator();

        public Line3d()
        {
            SpriteOrigin = OsbOrigin.CentreLeft;
        }

        public override void GenerateKeyframes(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var startVector = cameraState.ToScreen(wvp, StartPosition.ValueAt(time));
            var endVector = cameraState.ToScreen(wvp, EndPosition.ValueAt(time));

            var delta = startVector - endVector;
            var angle = Math.PI + Math.Atan2(delta.Y, delta.X);
            var rotation = InterpolatingFunctions.DoubleAngle(generator.EndState?.Rotation ?? 0, angle, 1);

            var scale = new Vector2((float)Math.Ceiling(delta.Xy.Length * 0.5f), Thickness);

            var opacity = startVector.W < 0 && endVector.W < 0 ? 0 : object3dState.Opacity;

            generator.Add(new CommandGenerator.State()
            {
                Time = time,
                Position = new Vector2((float)Math.Round(startVector.X * 10) / 10, (float)Math.Round(startVector.Y * 10) / 10),
                Scale = new Vector2((float)Math.Round(scale.X * 100) / 100f, (float)Math.Round(scale.Y * 100) / 100f),
                Rotation = (float)Math.Round(rotation * 1000) / 1000,
                Color = object3dState.Color,
                Opacity = (float)Math.Round(opacity * 100f) / 100,
            });
        }

        public override void GenerateSprite(StoryboardLayer layer, double startTime, double endTime)
        {
            var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(SpritePath);

            var sprite = layer.CreateSprite(SpritePath, SpriteOrigin);
            if (generator.GenerateCommands(sprite, bitmap.Width, bitmap.Height))
            {
                if (Additive)
                    sprite.Additive(sprite.CommandsStartTime, sprite.CommandsEndTime);
            }
        }
    }
}
