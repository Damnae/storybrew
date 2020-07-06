using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers;
using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using System;
using System.IO;

namespace StorybrewEditor.Storyboarding
{
    public class EditorOsbSprite : OsbSprite, DisplayableObject
    {
        public readonly static RenderStates AlphaBlendStates = new RenderStates();
        public readonly static RenderStates AdditiveStates = new RenderStates() { BlendingFactor = new BlendingFactorState(BlendingMode.Additive), };

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project, FrameStats frameStats)
            => Draw(drawContext, camera, bounds, opacity, project, frameStats, this);

        public static void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project, FrameStats frameStats, OsbSprite sprite)
        {
            var time = project.DisplayTime * 1000;
            if (sprite.TexturePath == null || !sprite.IsActive(time)) return;

            if (frameStats != null)
            {
                frameStats.SpriteCount++;
                frameStats.CommandCount += sprite.CommandCost;
                frameStats.IncompatibleCommands |= sprite.HasIncompatibleCommands;
                frameStats.OverlappedCommands |= sprite.HasOverlappedCommands;
            }

            var fade = sprite.OpacityAt(time);
            if (fade < 0.00001f) return;

            var scale = (Vector2)sprite.ScaleAt(time);
            if (scale.X == 0 || scale.Y == 0) return;
            if (sprite.FlipHAt(time)) scale.X = -scale.X;
            if (sprite.FlipVAt(time)) scale.Y = -scale.Y;

            var fullPath = Path.Combine(project.MapsetPath, sprite.GetTexturePathAt(time));
            Texture2dRegion texture = null;
            try
            {
                texture = project.TextureContainer.Get(fullPath);
            }
            catch (IOException)
            {
                // Happens when another process is writing to the file, will try again later.
                return;
            }
            if (texture == null) return;

            var position = sprite.PositionAt(time);
            var rotation = sprite.RotationAt(time);
            var color = sprite.ColorAt(time);
            var finalColor = ((Color4)color)
                .LerpColor(Color4.Black, project.DimFactor)
                .WithOpacity(opacity * fade);
            var additive = sprite.AdditiveAt(time);

            var origin = GetOriginVector(sprite.Origin, texture.Width, texture.Height);

            if (frameStats != null)
            {
                var size = texture.Size * scale;
                if (InScreenBounds(position, size, rotation, origin * scale))
                {
                    frameStats.EffectiveCommandCount += sprite.CommandCost;

                    var oneScreen = OsuHitObject.StoryboardSize.X * OsuHitObject.StoryboardSize.Y;
                    var oneWidescreen = OsuHitObject.WidescreenStoryboardSize.X * OsuHitObject.WidescreenStoryboardSize.Y;
                    frameStats.DrawLoad += Math.Min(size.X * size.Y, oneWidescreen) / oneScreen;
                }
            }

            var boundsScaling = bounds.Height / 480;
            DrawState.Prepare(drawContext.Get<SpriteRenderer>(), camera, additive ? AdditiveStates : AlphaBlendStates)
                .Draw(texture, bounds.Left + bounds.Width * 0.5f + (position.X - 320) * boundsScaling, bounds.Top + position.Y * boundsScaling,
                    origin.X, origin.Y, scale.X * boundsScaling, scale.Y * boundsScaling, rotation, finalColor);
        }
    }
}
