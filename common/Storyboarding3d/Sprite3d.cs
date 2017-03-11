using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class Sprite3d : Node3d
    {
        public string SpritePath;
        public bool Additive;

        public readonly KeyframedValue<Vector2> SpriteScale = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2, Vector2.One);

        private readonly CommandGenerator generator = new CommandGenerator();

        public override void GenerateKeyframes(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var screenPosition = cameraState.ToScreen(wvp, Vector3.Zero);
            var unitXPosition = cameraState.ToScreen(wvp, Vector3.UnitX);

            var delta = unitXPosition - screenPosition;
            var angle = Math.Atan2(delta.Y, delta.X);
            var rotation = InterpolatingFunctions.DoubleAngle(generator.EndState?.Rotation ?? 0, angle, 1);

            var scale = SpriteScale.ValueAt(time)
                * object3dState.WorldTransform.ExtractScale().Xy
                * (float)(cameraState.FocusDistance / screenPosition.W)
                * (float)cameraState.ResolutionScale;

            var opacity = screenPosition.W < 0 ? 0 : object3dState.Opacity;

            generator.Add(new CommandGenerator.State()
            {
                Time = time,
                Position = new Vector2((float)Math.Round(screenPosition.X * 10) / 10, (float)Math.Round(screenPosition.Y * 10) / 10),
                Scale = new Vector2((float)Math.Round(scale.X * 100) / 100f, (float)Math.Round(scale.Y * 100) / 100f),
                Rotation = (float)Math.Round(rotation * 1000f) / 1000,
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
