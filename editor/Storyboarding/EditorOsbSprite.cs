using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.Util;
using System.IO;

namespace StorybrewEditor.Storyboarding
{
    public class EditorOsbSprite : OsbSprite, DisplayableObject
    {
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project)
        {
            var time = project.DisplayTime * 1000;
            if (!IsActive(time)) return;

            var fade = OpacityAt(time);
            if (fade < 0.00001f) return;

            var scale = (Vector2)ScaleAt(time);
            if (scale.X == 0 || scale.Y == 0) return;
            if (FlipHAt(time)) scale.X = -scale.X;
            if (FlipVAt(time)) scale.Y = -scale.Y;

            var fullPath = Path.Combine(project.MapsetPath, TexturePath);
            Texture2d texture = null;
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

            var position = PositionAt(time);
            var rotation = RotationAt(time);
            var color = ColorAt(time);
            var finalColor = ((Color4)color).WithOpacity(opacity * fade);
            var additive = AdditiveAt(time);

            Vector2 origin;
            switch (Origin)
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

            var renderer = drawContext.SpriteRenderer;
            DrawState.Renderer = renderer;
            DrawState.SetBlending(additive ? BlendingMode.Additive : BlendingMode.Default);
            renderer.Camera = camera;
            renderer.Draw(texture, bounds.Left + bounds.Width * 0.5f + (position.X - 320) * boundsScaling, bounds.Top + position.Y * boundsScaling, origin.X, origin.Y, scale.X * boundsScaling, scale.Y * boundsScaling, rotation, finalColor);
        }
    }
}
