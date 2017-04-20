#if DEBUG
using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;

namespace StorybrewCommon.Storyboarding3d
{
    public class Sprite3d : Node3d, HasOsbSprite
    {
        public OsbSprite Sprite { get; private set; }
        public string SpritePath;
        public OsbOrigin SpriteOrigin = OsbOrigin.Centre;
        public bool Additive;
        public RotationMode RotationMode = RotationMode.UnitY;
        public bool UseDistanceFade = true;

        public readonly KeyframedValue<Vector2> SpriteScale = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2, Vector2.One);
        public readonly KeyframedValue<double> SpriteRotation = new KeyframedValue<double>(InterpolatingFunctions.DoubleAngle, 0);

        public readonly CommandGenerator Generator = new CommandGenerator();

        public override void GenerateSprite(StoryboardLayer layer)
        {
            Sprite = Sprite ?? layer.CreateSprite(SpritePath, SpriteOrigin);
        }

        public override void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var screenPosition = cameraState.ToScreen(wvp, Vector3.Zero);

            var angle = 0.0;
            switch (RotationMode)
            {
                case RotationMode.UnitX:
                    {
                        var unitXPosition = cameraState.ToScreen(wvp, Vector3.UnitX);
                        var delta = unitXPosition - screenPosition;
                        angle += (float)Math.Atan2(delta.Y, delta.X);
                    }
                    break;
                case RotationMode.UnitY:
                    {
                        var unitYPosition = cameraState.ToScreen(wvp, Vector3.UnitY);
                        var delta = unitYPosition - screenPosition;
                        angle += (float)Math.Atan2(delta.Y, delta.X) - Math.PI * 0.5;
                    }
                    break;
            }

            var previousState = Generator.EndState;
            var rotation = InterpolatingFunctions.DoubleAngle(previousState?.Rotation ?? 0 - SpriteRotation.ValueAt(previousState?.Time ?? time), angle, 1) + SpriteRotation.ValueAt(time);

            var scale = SpriteScale.ValueAt(time)
                * object3dState.WorldTransform.ExtractScale().Xy
                * (float)(cameraState.FocusDistance / screenPosition.W)
                * (float)cameraState.ResolutionScale;

            var opacity = screenPosition.W < 0 ? 0 : object3dState.Opacity;
            if (UseDistanceFade) opacity *= cameraState.OpacityAt(screenPosition.W);

            Generator.Add(new CommandGenerator.State()
            {
                Time = time,
                Position = screenPosition.Xy,
                Scale = scale,
                Rotation = rotation,
                Color = object3dState.Color,
                Opacity = opacity,
            });
        }

        public override void GenerateCommands(Action<Action, OsbSprite> action, double timeOffset)
        {
            if (Generator.GenerateCommands(Sprite, action, timeOffset))
            {
                if (Additive)
                    Sprite.Additive(Sprite.CommandsStartTime, Sprite.CommandsEndTime);
            }
        }
    }

    public enum RotationMode
    {
        Fixed,
        UnitX,
        UnitY,
    }
}
#endif