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
    public class Line3dEx : Object3d, HasOsbSprites
    {
        public OsbSprite spriteBody;
        public OsbSprite spriteEdgeTop;
        public OsbSprite spriteEdgeBottom;
        public OsbSprite spriteCapLeft;
        public OsbSprite spriteCapRight;
        public IEnumerable<OsbSprite> Sprites
        {
            get
            {
                yield return spriteBody;
                if (SpritePathEdge != null)
                {
                    yield return spriteEdgeTop;
                    yield return spriteEdgeBottom;
                }
                if (SpritePathCap != null)
                {
                    yield return spriteCapLeft;
                    yield return spriteCapRight;
                }
            }
        }

        public string SpritePathBody;
        public string SpritePathEdge;
        public string SpritePathCap;
        public bool Additive;
        public bool UseDistanceFade = true;
        public bool EnableLeftCap = true;
        public bool EnableRightCap = true;

        public readonly KeyframedValue<Vector3> StartPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<Vector3> EndPosition = new KeyframedValue<Vector3>(InterpolatingFunctions.Vector3);
        public readonly KeyframedValue<float> Thickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<float> StartThickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);
        public readonly KeyframedValue<float> EndThickness = new KeyframedValue<float>(InterpolatingFunctions.Float, 1);

        public readonly CommandGenerator GeneratorBody = new CommandGenerator();
        public readonly CommandGenerator GeneratorTop = new CommandGenerator();
        public readonly CommandGenerator GeneratorBottom = new CommandGenerator();
        public readonly CommandGenerator GeneratorLeft = new CommandGenerator();
        public readonly CommandGenerator GeneratorRight = new CommandGenerator();

        public override void GenerateSprite(StoryboardLayer layer)
        {
            spriteBody = spriteBody ?? layer.CreateSprite(SpritePathBody, OsbOrigin.Centre);
            if (SpritePathEdge != null)
            {
                spriteEdgeTop = spriteEdgeTop ?? layer.CreateSprite(SpritePathEdge, OsbOrigin.BottomCentre);
                spriteEdgeBottom = spriteEdgeBottom ?? layer.CreateSprite(SpritePathEdge, OsbOrigin.TopCentre);
            }
            if (SpritePathCap != null)
            {
                spriteCapLeft = spriteCapLeft ?? layer.CreateSprite(SpritePathCap, OsbOrigin.Centre);
                spriteCapRight = spriteCapRight ?? layer.CreateSprite(SpritePathCap, OsbOrigin.Centre);
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
            });

            // Edges

            if (SpritePathEdge != null)
            {
                var bitmapEdge = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteEdgeTop.GetTexturePathAt(time));
                var edgeScale = new Vector2(length / bitmapEdge.Width, edgeHeight / bitmapEdge.Height);

                var edgeOffset = new Vector2((float)Math.Cos(angle - Math.PI * 0.5), (float)Math.Sin(angle - Math.PI * 0.5)) * (bodyHeight * 0.5f - 0.5f);
                var positionTop = positionBody + edgeOffset;
                var positionBottom = positionBody - edgeOffset;

                var edgeOpacity = edgeHeight > 0.5f ? opacity : 0;

                GeneratorTop.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = positionTop,
                    Scale = edgeScale,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = edgeOpacity,
                    FlipH = flip,
                });
                GeneratorBottom.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = positionBottom,
                    Scale = edgeScale,
                    Rotation = rotation,
                    Color = object3dState.Color,
                    Opacity = edgeOpacity,
                    FlipH = flip,
                    FlipV = true,
                });
            }

            // Caps 

            if (SpritePathCap != null)
            {
                var bitmapCap = StoryboardObjectGenerator.Current.GetMapsetBitmap(spriteCapLeft.GetTexturePathAt(time));
                var leftCapScale = new Vector2(startScale / bitmapCap.Width, startScale / bitmapCap.Height);
                var rightCapScale = new Vector2(endScale / bitmapCap.Width, endScale / bitmapCap.Height);

                var leftCapOpacity = startScale > 0.5f ? opacity : 0;
                var rightCapOpacity = endScale > 0.5f ? opacity : 0;

                GeneratorLeft.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = startVector.Xy,
                    Scale = leftCapScale,
                    Rotation = 0,
                    Color = object3dState.Color,
                    Opacity = leftCapOpacity,
                });
                GeneratorRight.Add(new CommandGenerator.State()
                {
                    Time = time,
                    Position = endVector.Xy,
                    Scale = rightCapScale,
                    Rotation = 0,
                    Color = object3dState.Color,
                    Opacity = rightCapOpacity,
                });
            }
        }

        public override void GenerateCommands(Action<Action, OsbSprite> action, double timeOffset)
        {
            if (GeneratorBody.GenerateCommands(spriteBody, action, timeOffset))
                if (Additive) spriteBody.Additive(spriteBody.CommandsStartTime, spriteBody.CommandsEndTime);

            if (SpritePathEdge != null)
            {
                if (GeneratorTop.GenerateCommands(spriteEdgeTop, action, timeOffset))
                    if (Additive) spriteEdgeTop.Additive(spriteEdgeTop.CommandsStartTime, spriteEdgeTop.CommandsEndTime);
                if (GeneratorBottom.GenerateCommands(spriteEdgeBottom, action, timeOffset))
                    if (Additive) spriteEdgeBottom.Additive(spriteEdgeBottom.CommandsStartTime, spriteEdgeBottom.CommandsEndTime);
            }

            if (SpritePathCap != null)
            {
                if (EnableLeftCap && GeneratorLeft.GenerateCommands(spriteCapLeft, action, timeOffset))
                    if (Additive) spriteCapLeft.Additive(spriteCapLeft.CommandsStartTime, spriteCapLeft.CommandsEndTime);
                if (EnableRightCap && GeneratorRight.GenerateCommands(spriteCapRight, action, timeOffset))
                    if (Additive) spriteCapRight.Additive(spriteCapRight.CommandsStartTime, spriteCapRight.CommandsEndTime);
            }
        }
    }
}
#endif