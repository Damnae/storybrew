using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding3d
{
#pragma warning disable CS1591
    public class Line3d : Node3d, HasOsbSprites
    {
        Action<OsbSprite> finalize;
        OsbSprite sprite;

        public IEnumerable<OsbSprite> Sprites { get { yield return sprite; } }
        public string SpritePath;
        public OsbOrigin SpriteOrigin = OsbOrigin.CentreLeft;

        public bool Additive, UseDistanceFade = true;

        public readonly KeyframedValue<Vector3> 
            StartPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3), 
            EndPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<float> Thickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        readonly CommandGenerator Generator = new CommandGenerator();
        public override IEnumerable<CommandGenerator> CommandGenerators { get { yield return Generator; } }

        public override void GenerateSprite(StoryboardSegment segment) => sprite = sprite ?? segment.CreateSprite(SpritePath, SpriteOrigin);
        public override void GenerateStates(double time, CameraState cameraState, Object3dState object3dState)
        {
            var wvp = object3dState.WorldTransform * cameraState.ViewProjection;
            var startVector = cameraState.ToScreen(wvp, StartPosition.ValueAt(time));
            var endVector = cameraState.ToScreen(wvp, EndPosition.ValueAt(time));

            var delta = endVector.Xy - startVector.Xy;
            var length = delta.Length;
            if (Math.Round(length, Generator.ScaleDecimals) == 0) return;

            var angle = Math.Atan2(delta.Y, delta.X);
            var rotation = InterpolatingFunctions.DoubleAngle(Generator.EndState?.Rotation ?? 0, angle, 1);

            var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite.TexturePath);
            var scale = new Vector2(length / bitmap.Width, Thickness.ValueAt(time));

            var opacity = startVector.W < 0 && endVector.W < 0 ? 0 : object3dState.Opacity;
            if (UseDistanceFade) opacity *= Math.Max(cameraState.OpacityAt(startVector.W), cameraState.OpacityAt(endVector.W));

            Vector2 position;
            switch (sprite.Origin)
            {
                default:
                case OsbOrigin.TopLeft:
                case OsbOrigin.CentreLeft:
                case OsbOrigin.BottomLeft: position = startVector.Xy; break;
                case OsbOrigin.TopCentre: case OsbOrigin.Centre: case OsbOrigin.BottomCentre: position = startVector.Xy + delta / 2; break;
                case OsbOrigin.TopRight: case OsbOrigin.CentreRight: case OsbOrigin.BottomRight: position = endVector.Xy; break;
            }
            Generator.Add(new State
            {
                Time = time,
                Position = position,
                Scale = scale,
                Rotation = rotation,
                Color = object3dState.Color,
                Opacity = opacity,
                Additive = Additive
            });
        }

        ///<inheritdoc/>
        public void DoTreeSprite(Action<OsbSprite> action = null) => finalize = action;
        public override void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable)
        {
            Generator.GenerateCommands(sprite, action, startTime, endTime, timeOffset, loopable);
            finalize?.Invoke(sprite);
        }
    }
    public class Line3dEx : Node3d, HasOsbSprites
    {
        Action<OsbSprite> finalize;
        OsbSprite spriteBody, spriteTopEdge, spriteBottomEdge, spriteStartCap, spriteEndCap;

        public IEnumerable<OsbSprite> Sprites
        {
            get
            {
                yield return spriteBody;
                if (SpritePathEdge != null)
                {
                    yield return spriteTopEdge;
                    yield return spriteBottomEdge;
                }
                if (SpritePathCap != null)
                {
                    yield return spriteStartCap;
                    yield return spriteEndCap;
                }
            }
        }

        public string SpritePathBody, SpritePathEdge, SpritePathCap;
        public bool Additive, UseDistanceFade = true, EnableStartCap = true, EnableEndCap = true, OrientedCaps;

        public float EdgeOverlap = .5f, CapOverlap = .2f;

        public readonly KeyframedValue<Vector3> 
            StartPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3),
            EndPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<float> 
            Thickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1), 
            StartThickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1), 
            EndThickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        readonly CommandGenerator 
            GeneratorBody = new CommandGenerator(), 
            GeneratorTopEdge = new CommandGenerator(), 
            GeneratorBottomEdge = new CommandGenerator(),
            GeneratorStartCap = new CommandGenerator(),
            GeneratorEndCap = new CommandGenerator();
        public override IEnumerable<CommandGenerator> CommandGenerators
        {
            get
            {
                yield return GeneratorBody;
                yield return GeneratorTopEdge;
                yield return GeneratorBottomEdge;
                yield return GeneratorStartCap;
                yield return GeneratorEndCap;
            }
        }

        public override void GenerateSprite(StoryboardSegment segment)
        {
            spriteBody = spriteBody ?? segment.CreateSprite(SpritePathBody, OsbOrigin.Centre);
            if (SpritePathEdge != null)
            {
                spriteTopEdge = spriteTopEdge ?? segment.CreateSprite(SpritePathEdge, OsbOrigin.BottomCentre);
                spriteBottomEdge = spriteBottomEdge ?? segment.CreateSprite(SpritePathEdge, OsbOrigin.TopCentre);
            }
            if (SpritePathCap != null)
            {
                spriteStartCap = spriteStartCap ?? segment.CreateSprite(SpritePathCap, OrientedCaps ? OsbOrigin.CentreLeft : OsbOrigin.Centre);
                spriteEndCap = spriteEndCap ?? segment.CreateSprite(SpritePathCap, OrientedCaps ? OsbOrigin.CentreRight : OsbOrigin.Centre);
            }
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
            var rotation = InterpolatingFunctions.DoubleAngle(GeneratorBody.EndState?.Rotation ?? 0, angle, 1);

            var thickness = Thickness.ValueAt(time);
            var scaleFactor = object3dState.WorldTransform.ExtractScale().Y * (float)cameraState.ResolutionScale;
            var startScale = scaleFactor * (float)(cameraState.FocusDistance / startVector.W) * thickness * StartThickness.ValueAt(time);
            var endScale = scaleFactor * (float)(cameraState.FocusDistance / endVector.W) * thickness * EndThickness.ValueAt(time);

            var totalHeight = Math.Max(startScale, endScale);
            var bodyHeight = Math.Min(startScale, endScale);
            var edgeHeight = (totalHeight - bodyHeight) / 2;
            var flip = startScale < endScale;

            var ignoreEdges = edgeHeight < EdgeOverlap;
            if (ignoreEdges) bodyHeight += edgeHeight * 2;

            var opacity = startVector.W < 0 && endVector.W < 0 ? 0 : object3dState.Opacity;
            if (UseDistanceFade) opacity *= Math.Max(cameraState.OpacityAt(startVector.W), cameraState.OpacityAt(endVector.W));

            var bitmapBody = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteBody.TexturePath);
            var bodyScale = new Vector2(length / bitmapBody.Width, bodyHeight / bitmapBody.Height);

            var positionBody = startVector.Xy + delta / 2;
            var bodyOpacity = opacity;

            GeneratorBody.Add(new State
            {
                Time = time,
                Position = positionBody,
                Scale = bodyScale,
                Rotation = rotation,
                Color = object3dState.Color,
                Opacity = bodyOpacity,
                Additive = Additive
            });

            if (SpritePathEdge != null)
            {
                var bitmapEdge = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteTopEdge.TexturePath);
                var edgeScale = new Vector2(length / bitmapEdge.Width, edgeHeight / bitmapEdge.Height);

                var edgeOffset = new Vector2((float)Math.Cos(angle - Math.PI / 2), (float)Math.Sin(angle - Math.PI / 2)) * (bodyHeight / 2 - EdgeOverlap);
                var positionTop = positionBody + edgeOffset;
                var positionBottom = positionBody - edgeOffset;

                var edgeOpacity = ignoreEdges ? 0 : opacity;

                GeneratorTopEdge.Add(new State
                {
                    Time = time,
                    Position = positionTop,
                    Scale = edgeScale,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = edgeOpacity,
                    FlipH = flip,
                    Additive = Additive
                });
                GeneratorBottomEdge.Add(new State
                {
                    Time = time,
                    Position = positionBottom,
                    Scale = edgeScale,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = edgeOpacity,
                    Additive = Additive,
                    FlipH = flip,
                    FlipV = true
                });
            }
            if (SpritePathCap != null)
            {
                var bitmapCap = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteStartCap.TexturePath);
                var startCapScale = new Vector2(startScale / bitmapCap.Width, startScale / bitmapCap.Height);
                var endCapScale = new Vector2(endScale / bitmapCap.Width, endScale / bitmapCap.Height);

                var capOffset = OrientedCaps ? new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * CapOverlap : Vector2.Zero;

                if (OrientedCaps)
                {
                    startCapScale.X /= 2;
                    endCapScale.X /= 2;
                }

                var startCapOpacity = startScale > .5 ? opacity : 0;
                var endCapOpacity = endScale > .5 ? opacity : 0;

                GeneratorStartCap.Add(new State
                {
                    Time = time,
                    Position = startVector.Xy + capOffset,
                    Scale = startCapScale,
                    Rotation = OrientedCaps ? rotation + Math.PI : 0,
                    Color = object3dState.Color,
                    Opacity = startCapOpacity,
                    Additive = Additive
                });
                GeneratorEndCap.Add(new State
                {
                    Time = time,
                    Position = endVector.Xy - capOffset,
                    Scale = endCapScale,
                    Rotation = OrientedCaps ? rotation + Math.PI : 0,
                    Color = object3dState.Color,
                    Opacity = endCapOpacity,
                    Additive = Additive,
                    FlipH = OrientedCaps
                });
            }
        }

        ///<inheritdoc/>
        public void DoTreeSprite(Action<OsbSprite> action = null) => finalize = action;
        public override void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable)
        {
            if (finalize != null)
            {
                Action<Action, OsbSprite> action2 = (createCommands, sprite) => finalize(sprite);
                action += action2;
            }
            
            GeneratorBody.GenerateCommands(spriteBody, action, startTime, endTime, timeOffset, loopable);
            if (SpritePathEdge != null)
            {
                GeneratorTopEdge.GenerateCommands(spriteTopEdge, action, startTime, endTime, timeOffset, loopable);
                GeneratorBottomEdge.GenerateCommands(spriteBottomEdge, action, startTime, endTime, timeOffset, loopable);
            }
            if (SpritePathCap != null)
            {
                if (EnableStartCap) GeneratorStartCap.GenerateCommands(spriteStartCap, action, startTime, endTime, timeOffset, loopable);
                if (EnableEndCap) GeneratorEndCap.GenerateCommands(spriteEndCap, action, startTime, endTime, timeOffset, loopable);
            }
        }
    }
}
