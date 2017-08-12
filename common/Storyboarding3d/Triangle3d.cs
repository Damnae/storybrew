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
    public class Triangle3d : Node3d, HasOsbSprites
    {
        public OsbSprite sprite0;
        public OsbSprite sprite1;
        public IEnumerable<OsbSprite> Sprites { get { yield return sprite0; yield return sprite1; } }

        public string SpritePath;
        public bool Additive;
        public bool UseDistanceFade = true;

        public readonly KeyframedValue<Vector3> Position0 = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> Position1 = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> Position2 = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);

        public readonly CommandGenerator Generator0 = new CommandGenerator();
        public readonly CommandGenerator Generator1 = new CommandGenerator();

        public override void GenerateSprite(StoryboardLayer layer)
        {
            sprite0 = sprite0 ?? layer.CreateSprite(SpritePath, OsbOrigin.BottomLeft);
            sprite1 = sprite1 ?? layer.CreateSprite(SpritePath, OsbOrigin.BottomRight);
        }

        public override void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var vector0 = cameraState.ToScreen(wvp, Position0.ValueAt(time));
            var vector1 = cameraState.ToScreen(wvp, Position1.ValueAt(time));
            var vector2 = cameraState.ToScreen(wvp, Position2.ValueAt(time));

            var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite0.GetTexturePathAt(time));

            for (var i = 0; i < 3; i++)
            {
                var cross = (vector2.X - vector0.X) * (vector1.Y - vector0.Y)
                    - (vector2.Y - vector0.Y) * (vector1.X - vector0.X);

                if (cross > 0)
                {
                    if (Generator0.EndState != null)
                        Generator0.Add(new CommandGenerator.State()
                        {
                            Time = time,
                            Position = Generator0.EndState.Position,
                            Scale = Generator0.EndState.Scale,
                            Rotation = Generator0.EndState.Rotation,
                            Color = object3dState.Color,
                            Opacity = 0,
                        });
                    if (Generator1.EndState != null)
                        Generator1.Add(new CommandGenerator.State()
                        {
                            Time = time,
                            Position = Generator1.EndState.Position,
                            Scale = Generator1.EndState.Scale,
                            Rotation = Generator1.EndState.Rotation,
                            Color = object3dState.Color,
                            Opacity = 0,
                            FlipH = true,
                        });
                    break;
                }

                var delta = vector2.Xy - vector0.Xy;
                var deltaLength = delta.Length;
                var normalizedDelta = delta / deltaLength;

                var delta2 = vector1.Xy - vector0.Xy;
                var dot = Vector2.Dot(normalizedDelta, delta2);
                if (dot <= 0 || dot > deltaLength)
                {
                    var temp = vector0;
                    vector0 = vector1;
                    vector1 = vector2;
                    vector2 = temp;
                    continue;
                }

                var position = project(vector0.Xy, vector2.Xy, vector1.Xy);
                var scale0 = new Vector2((vector2.Xy - position).Length / bitmap.Width, (vector1.Xy - position).Length / bitmap.Height);
                var scale1 = new Vector2((vector0.Xy - position).Length / bitmap.Width, scale0.Y);

                var angle = Math.Atan2(delta.Y, delta.X);
                var rotation = InterpolatingFunctions.DoubleAngle(Generator0.EndState?.Rotation ?? 0, angle, 1);

                var opacity = vector0.W < 0 && vector1.W < 0 && vector2.W < 0 ? 0 : object3dState.Opacity;
                if (UseDistanceFade) opacity *= (cameraState.OpacityAt(vector0.W) + cameraState.OpacityAt(vector1.W) + cameraState.OpacityAt(vector2.W)) / 3;

                Generator0.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = position,
                    Scale = scale0,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = opacity,
                });
                Generator1.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = position,
                    Scale = scale1,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = opacity,
                    FlipH = true,
                });
                break;
            }
        }

        public override void GenerateCommands(Action<Action, OsbSprite> action, double timeOffset)
        {
            if (Generator0.GenerateCommands(sprite0, action, timeOffset))
                if (Additive) sprite0.Additive(sprite0.CommandsStartTime, sprite0.CommandsEndTime);
            if (Generator1.GenerateCommands(sprite1, action, timeOffset))
                if (Additive) sprite1.Additive(sprite1.CommandsStartTime, sprite1.CommandsEndTime);
        }

        private static Vector2 project(Vector2 line1, Vector2 line2, Vector2 toProject)
        {
            var m = (line2.Y - line1.Y) / (line2.X - line1.X);
            var b = line1.Y - (m * line1.X);

            var x = (m * toProject.Y + toProject.X - m * b) / (m * m + 1);
            var y = (m * m * toProject.Y + m * toProject.X + b) / (m * m + 1);

            return new Vector2(x, y);
        }
    }
}
#endif