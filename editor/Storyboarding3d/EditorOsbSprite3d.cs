using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding3d;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.Util;
using System.IO;

namespace StorybrewEditor.Storyboarding3d
{
    public class EditorOsbSprite3d : OsbSprite3d, DisplayableObject3d
    {
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, Project project, StoryboardCamera.State cameraState, State3d parentState = null)
        {
            var time = project.DisplayTime * 1000;
            var state3d = GetState3d(time, parentState);
            var state2d = GetState2d(state3d, cameraState);

            //if (!sprite.IsActive(time)) return;

            var fade = state2d.Opacity;
            if (fade < 0.00001f) return;

            var scale = state2d.Scale;
            if (scale.X == 0 || scale.Y == 0) return;
            //if (sprite.FlipHAt(time)) scale.X = -scale.X;
            //if (sprite.FlipVAt(time)) scale.Y = -scale.Y;

            var fullPath = Path.Combine(project.MapsetPath, GetTexturePathAt(time));
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

            var position = state2d.Position;
            var additive = false;// sprite.AdditiveAt(time);
            var finalColor = ((Color4)state3d.Color).WithOpacity(fade);

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
            DrawState.Prepare(drawContext.SpriteRenderer, camera, additive ? EditorOsbSprite.AdditiveStates : EditorOsbSprite.AlphaBlendStates)
                .Draw(texture, bounds.Left + bounds.Width * 0.5f + (position.X - 320) * boundsScaling, bounds.Top + position.Y * boundsScaling,
                    origin.X, origin.Y, scale.X * boundsScaling, scale.Y * boundsScaling, state2d.Rotation, finalColor);
        }
    }
}
