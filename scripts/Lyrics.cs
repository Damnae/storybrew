using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Subtitles;

namespace StorybrewScripts
{
    public class Lyrics : StoryboardObjectGenerator
    {
        [Configurable]
        public string SubtitlesPath = "lyrics.srt";

        [Configurable]
        public string FontName = "Verdana";

        [Configurable]
        public string SpritesPath = "sb/f";

        [Configurable]
        public int FontSize = 26;

        [Configurable]
        public float FontScale = 0.5f;

        [Configurable]
        public int OutlineThickness = 3;

        [Configurable]
        public int ShadowThickness = 0;

        [Configurable]
        public float PaddingX = 0;

        [Configurable]
        public float PaddingY = 0;

        [Configurable]
        public float SubtitleY = 400;

        [Configurable]
        public bool Debug = false;

        public override void Generate()
        {
            var font = LoadFont(SpritesPath, new FontDescription()
            {
                FontPath = FontName,
                FontSize = FontSize,
                Color = Color4.White,
                Padding = new Vector2(PaddingX, PaddingY),
                Debug = Debug,
            },
            new FontOutline()
            {
                Thickness = OutlineThickness,
                Color = new Color4(30, 30, 30, 200),
            },
            new FontShadow()
            {
                Thickness = ShadowThickness,
                Color = new Color4(0, 0, 0, 100),
            });

            var layer = GetLayer("");
            var subtitles = LoadSubtitles(SubtitlesPath);
            foreach (var subtitleLine in subtitles.Lines)
            {
                var texture = font.GetTexture(subtitleLine.Text);
                var sprite = layer.CreateSprite(texture.Path, OsbOrigin.Centre, new Vector2(320, SubtitleY));
                sprite.Scale(subtitleLine.StartTime, FontScale);
                sprite.Fade(subtitleLine.StartTime - 200, subtitleLine.StartTime, 0, 1);
                sprite.Fade(subtitleLine.EndTime - 200, subtitleLine.EndTime, 1, 0);
            }
        }
    }
}
