using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Textures;
using System.IO;
using BrewLib.Util;
using BrewLib.Graphics.Renderers;

namespace StorybrewEditor.Storyboarding
{
    public class EditorOsbVideo : OsbVideo, DisplayableObject
    {
        public readonly static RenderStates AlphaBlendStates = new RenderStates();
        public readonly static RenderStates AdditiveStates = new RenderStates() { BlendingFactor = new BlendingFactorState(BlendingMode.Additive), };

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project)
            => Draw(drawContext, camera, bounds, opacity, project, this);

        public static void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project, OsbVideo video)
        {
            var time = project.DisplayTime * 1000;
            if (video.TexturePath == null || !video.IsActive(time)) return;

            var fade = video.OpacityAt(time);
            if (fade < 0.00001f) return;

            var scale = (Vector2)video.ScaleAt(time);
            if (scale.X == 0 || scale.Y == 0) return;
            if (video.FlipHAt(time)) scale.X = -scale.X;
            if (video.FlipVAt(time)) scale.Y = -scale.Y;

            var fullPath = Path.Combine(project.MapsetPath, video.GetTexturePathAt(time));
            Texture2d texture = null;
            
            try
            {
                texture = project.TextureContainer.Get(fullPath.Substring(0, fullPath.LastIndexOf('.')) + ".png");
            }
            catch (IOException)
            {
                // Happens when another process is writing to the file, will try again later.
                return;
            }
            if (texture == null) return;

            var position = video.PositionAt(time);
            var rotation = video.RotationAt(time);
            var color = video.ColorAt(time);
            var finalColor = ((Color4)color).WithOpacity(opacity * fade);
            var additive = video.AdditiveAt(time);

            Vector2 origin;
            switch (video.Origin)
            {
                default:
                case OsbOrigin.TopLeft: origin = new Vector2(0, 0); break;
                case OsbOrigin.TopCentre: origin = new Vector2(texture.Width * 0.5f, 0); break;
                case OsbOrigin.TopRight: origin = new Vector2(texture.Width, 0); break;
                case OsbOrigin.CentreLeft: origin = new Vector2(0, texture.Height * 0.5f); break;
                case OsbOrigin.Centre: origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f); break;
                case OsbOrigin.CentreRight: origin = new Vector2(texture.Width, texture.Height * 0.5f); break;
                case OsbOrigin.BottomLeft: origin = new Vector2(0, texture.Height); break;
                case OsbOrigin.BottomCentre: origin = new Vector2(texture.Width * 0.5f, texture.Height); break;
                case OsbOrigin.BottomRight: origin = new Vector2(texture.Width, texture.Height); break;
            }

            var boundsScaling = bounds.Height / 480;
            DrawState.Prepare(drawContext.Get<SpriteRenderer>(), camera, additive ? AdditiveStates : AlphaBlendStates)
                .Draw(texture, bounds.Left + bounds.Width * 0.5f + (position.X) * boundsScaling, bounds.Top + (position.Y + 240) * boundsScaling,
                    origin.X, origin.Y, scale.X * boundsScaling, scale.Y * boundsScaling, rotation, finalColor);
        }
    }
}
