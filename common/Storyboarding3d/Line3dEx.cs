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
    public class Line3dEx : Node3d, HasOsbSprites
    {
        public OsbSprite spriteBody;
        public OsbSprite spriteTopEdge;
        public OsbSprite spriteBottomEdge;
        public OsbSprite spriteStartCap;
        public OsbSprite spriteEndCapEnd;
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
                    yield return spriteEndCapEnd;
                }
            }
        }

        public string SpritePathBody;
        public string SpritePathEdge;
        public string SpritePathCap;
        public bool Additive;
        public bool UseDistanceFade = true;

        public float EdgeOverlap = 0.5f;
        public bool EnableStartCap = true;
        public bool EnableEndCap = true;
        public bool OrientedCaps;
        public float CapOverlap = 0.2f;

        public readonly KeyframedValue<Vector3> StartPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> EndPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<float> Thickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<float> StartThickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<float> EndThickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        public readonly CommandGenerator GeneratorBody = new CommandGenerator();
        public readonly CommandGenerator GeneratorTopEdge = new CommandGenerator();
        public readonly CommandGenerator GeneratorBottomEdge = new CommandGenerator();
        public readonly CommandGenerator GeneratorStartCap = new CommandGenerator();
        public readonly CommandGenerator GeneratorEndCap = new CommandGenerator();
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

        public override void GenerateSprite(StoryboardLayer layer)
        {
            spriteBody = spriteBody ?? layer.CreateSprite(SpritePathBody, OsbOrigin.Centre);
            if (SpritePathEdge != null)
            {
                spriteTopEdge = spriteTopEdge ?? layer.CreateSprite(SpritePathEdge, OsbOrigin.BottomCentre);
                spriteBottomEdge = spriteBottomEdge ?? layer.CreateSprite(SpritePathEdge, OsbOrigin.TopCentre);
            }
            if (SpritePathCap != null)
            {
                spriteStartCap = spriteStartCap ?? layer.CreateSprite(SpritePathCap, OrientedCaps ? OsbOrigin.CentreLeft : OsbOrigin.Centre);
                spriteEndCapEnd = spriteEndCapEnd ?? layer.CreateSprite(SpritePathCap, OrientedCaps ? OsbOrigin.CentreRight : OsbOrigin.Centre);
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
            var edgeHeight = (totalHeight - bodyHeight) * 0.5f;
            var flip = startScale < endScale;

            var ignoreEdges = edgeHeight < EdgeOverlap;
            if (ignoreEdges) bodyHeight += edgeHeight * 2;

            var opacity = startVector.W < 0 && endVector.W < 0 ? 0 : object3dState.Opacity;
            if (UseDistanceFade) opacity *= Math.Max(cameraState.OpacityAt(startVector.W), cameraState.OpacityAt(endVector.W));

            // Body

            var bitmapBody = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteBody.GetTexturePathAt(time));
            var bodyScale = new Vector2(length / bitmapBody.Width, bodyHeight / bitmapBody.Height);

            var positionBody = startVector.Xy + delta * 0.5f;
            var bodyOpacity = opacity;

            GeneratorBody.Add(new CommandGenerator.State()
            {
                Time = time,
                Position = positionBody,
                Scale = bodyScale,
                Rotation = rotation,
                Color = object3dState.Color,
                Opacity = bodyOpacity,
                Additive = Additive,
            });

            // Edges

            if (SpritePathEdge != null)
            {
                var bitmapEdge = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteTopEdge.GetTexturePathAt(time));
                var edgeScale = new Vector2(length / bitmapEdge.Width, edgeHeight / bitmapEdge.Height);

                var edgeOffset = new Vector2((float)Math.Cos(angle - Math.PI * 0.5), (float)Math.Sin(angle - Math.PI * 0.5)) * (bodyHeight * 0.5f - EdgeOverlap);
                var positionTop = positionBody + edgeOffset;
                var positionBottom = positionBody - edgeOffset;

                var edgeOpacity = ignoreEdges ? 0 : opacity;

                GeneratorTopEdge.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = positionTop,
                    Scale = edgeScale,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = edgeOpacity,
                    FlipH = flip,
                    Additive = Additive,
                });
                GeneratorBottomEdge.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = positionBottom,
                    Scale = edgeScale,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = edgeOpacity,
                    Additive = Additive,
                    FlipH = flip,
                    FlipV = true,
                });
            }

            // Caps 

            if (SpritePathCap != null)
            {
                var bitmapCap = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteStartCap.GetTexturePathAt(time));
                var startCapScale = new Vector2(startScale / bitmapCap.Width, startScale / bitmapCap.Height);
                var endCapScale = new Vector2(endScale / bitmapCap.Width, endScale / bitmapCap.Height);

                var capOffset = OrientedCaps ? new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * CapOverlap : Vector2.Zero;

                if (OrientedCaps)
                {
                    startCapScale.X *= 0.5f;
                    endCapScale.X *= 0.5f;
                }

                var startCapOpacity = startScale > 0.5f ? opacity : 0;
                var endCapOpacity = endScale > 0.5f ? opacity : 0;

                GeneratorStartCap.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = startVector.Xy + capOffset,
                    Scale = startCapScale,
                    Rotation = OrientedCaps ? rotation + Math.PI : 0,
                    Color = object3dState.Color,
                    Opacity = startCapOpacity,
                    Additive = Additive,
                });
                GeneratorEndCap.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = endVector.Xy - capOffset,
                    Scale = endCapScale,
                    Rotation = OrientedCaps ? rotation + Math.PI : 0,
                    Color = object3dState.Color,
                    Opacity = endCapOpacity,
                    Additive = Additive,
                    FlipH = OrientedCaps,
                });
            }
        }

        public override void GenerateCommands(Action<Action, OsbSprite> action, double? startTime, double? endTime, double timeOffset, bool loopable)
        {
            GeneratorBody.GenerateCommands(spriteBody, action, startTime, endTime, timeOffset, loopable);
            if (SpritePathEdge != null)
            {
                GeneratorTopEdge.GenerateCommands(spriteTopEdge, action, startTime, endTime, timeOffset, loopable);
                GeneratorBottomEdge.GenerateCommands(spriteBottomEdge, action, startTime, endTime, timeOffset, loopable);
            }
            if (SpritePathCap != null)
            {
                if (EnableStartCap) GeneratorStartCap.GenerateCommands(spriteStartCap, action, startTime, endTime, timeOffset, loopable);
                if (EnableEndCap) GeneratorEndCap.GenerateCommands(spriteEndCapEnd, action, startTime, endTime, timeOffset, loopable);
            }
        }
    }
}
#endif