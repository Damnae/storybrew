#if DEBUG
using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding3d
{
    public class Line3d : Node3d, HasOsbSprites
    {
        public OsbSprite sprite;
        public IEnumerable<OsbSprite> Sprites { get { yield return sprite; } }

        public string SpritePath;
        public OsbOrigin SpriteOrigin = OsbOrigin.CentreLeft;
        public bool Additive;
        public bool UseDistanceFade = true;
        
        public readonly KeyframedValue<Vector3> StartPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> EndPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<float> Thickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        public readonly CommandGenerator Generator = new CommandGenerator();
        public override IEnumerable<CommandGenerator> CommandGenerators { get { yield return Generator; } }

        public override void GenerateSprite(StoryboardLayer layer)
        {
            sprite = sprite ?? layer.CreateSprite(SpritePath, SpriteOrigin);
        }

        public override void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var startVector = cameraState.ToScreen(wvp, StartPosition.ValueAt(time));
            var endVector = cameraState.ToScreen(wvp, EndPosition.ValueAt(time));

            var delta = endVector.Xy - startVector.Xy;
            var length = delta.Length;
            if (length == 0) return;

            var angle = Math.Atan2(delta.Y, delta.X);
            var rotation = InterpolatingFunctions.DoubleAngle(Generator.EndState?.Rotation ?? 0, angle, 1);

            var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite.GetTexturePathAt(time));
            var scale = new Vector2(length / bitmap.Width, Thickness.ValueAt(time));

            var opacity = startVector.W < 0 && endVector.W < 0 ? 0 : object3dState.Opacity;
            if (UseDistanceFade) opacity *= Math.Max(cameraState.OpacityAt(startVector.W), cameraState.OpacityAt(endVector.W));

            Vector2 position;
            switch (sprite.Origin)
            {
                default:
                case OsbOrigin.TopLeft:
                case OsbOrigin.CentreLeft:
                case OsbOrigin.BottomLeft:
                    position = startVector.Xy;
                    break;

                case OsbOrigin.TopCentre:
                case OsbOrigin.Centre: 
                case OsbOrigin.BottomCentre:
                    position = startVector.Xy + delta * 0.5f;
                    break;

                case OsbOrigin.TopRight:
                case OsbOrigin.CentreRight:
                case OsbOrigin.BottomRight:
                    position = endVector.Xy;
                    break;
            }

            Generator.Add(new CommandGenerator.State()
            {
                Time = time,
                Position = position,
                Scale = scale,
                Rotation = rotation,
                Color = object3dState.Color,
                Opacity = opacity,
                Additive = Additive,
            });
        }

        public override void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable)
        {
            Generator.GenerateCommands(sprite, action, startTime, endTime, timeOffset, loopable);
        }
    }
}
#endif