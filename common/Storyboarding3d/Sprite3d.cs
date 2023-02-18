using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding3d
{
#pragma warning disable CS1591
    public interface HasOsbSprites
    {
        IEnumerable<OsbSprite> Sprites { get; }
    }
    public class Sprite3d : Node3d, HasOsbSprites
    {
        OsbSprite sprite;

        ///<summary> Gets this 3D sprite's list of <see cref="OsbSprite"/>s. </summary>
        public IEnumerable<OsbSprite> Sprites { get { yield return sprite; } }

        ///<summary> The path to the image of this <see cref="Sprite3d"/>. </summary>
        public string SpritePath;

        ///<summary> The <see cref="OsbOrigin"/> of this <see cref="Sprite3d"/>. </summary>
        public OsbOrigin SpriteOrigin = OsbOrigin.Centre;

        ///<summary> Toggles additive blending on this <see cref="Sprite3d"/>. </summary>
        public bool Additive;

        ///<summary> Represents method of sprite rotation on this <see cref="Sprite3d"/>. </summary>
        public RotationMode RotationMode = RotationMode.UnitY;

        ///<summary> Whether to fade sprites based on distance from the <see cref="Camera"/>. </summary>
        public bool UseDistanceFade = true;

        ///<summary> A keyframed value representing this sprite's scale keyframes. </summary>
        public readonly KeyframedValue<Vector2> SpriteScale = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2, Vector2.One);

        ///<summary> A keyframed value representing this sprite's rotation keyframes. </summary>
        public readonly KeyframedValue<double> SpriteRotation = new KeyframedValue<double>(InterpolatingFunctions.DoubleAngle, 0);

        readonly CommandGenerator gen = new CommandGenerator();

        ///<summary> Gets this sprite's <see cref="CommandGenerator"/>s. </summary>
        public override IEnumerable<CommandGenerator> CommandGenerators { get { yield return gen; } }

        public override void GenerateSprite(StoryboardSegment segment) => sprite = sprite ?? segment.CreateSprite(SpritePath, SpriteOrigin);
        public override void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var screenPosition = cameraState.ToScreen(wvp, Vector3.Zero);

            var angle = 0d;
            switch (RotationMode)
            {
                case RotationMode.UnitX:
                {
                    var unitXPosition = cameraState.ToScreen(wvp, Vector3.UnitX);
                    var delta = unitXPosition - screenPosition;
                    angle += Math.Atan2(delta.Y, delta.X);
                    break;
                }
                case RotationMode.UnitY:
                {
                    var unitYPosition = cameraState.ToScreen(wvp, Vector3.UnitY);
                    var delta = unitYPosition - screenPosition;
                    angle += Math.Atan2(delta.Y, delta.X) - Math.PI / 2;
                    break;
                }
            }

            var previousState = gen.EndState;
            var rotation = InterpolatingFunctions.DoubleAngle(
                previousState?.Rotation ?? -SpriteRotation.ValueAt(previousState?.Time ?? time), angle, 1) + SpriteRotation.ValueAt(time);

            var scale = SpriteScale.ValueAt(time) *
                object3dState.WorldTransform.ExtractScale().Xy *
                (float)(cameraState.FocusDistance / screenPosition.W) *
                (float)cameraState.ResolutionScale;

            var opacity = screenPosition.W < 0 ? 0 : object3dState.Opacity;
            if (UseDistanceFade) opacity *= cameraState.OpacityAt(screenPosition.W);

            gen.Add(new CommandGenerator.State
            {
                Time = time,
                Position = screenPosition.Xy,
                Scale = scale,
                Rotation = rotation,
                Color = object3dState.Color,
                Opacity = opacity,
                Additive = Additive,
                UseDistanceFade = UseDistanceFade
            });
        }
        public override void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable)
            => gen.GenerateCommands(sprite, action, startTime, endTime, timeOffset, loopable);
    }

    ///<summary> Represents the rotation method for a 3D sprite. </summary>
    public enum RotationMode
    {
        Fixed, UnitX, UnitY
    }
}