using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Subtitles;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace StorybrewScripts
{
    class Karaoke : StoryboardObjectGenerator
    {
        [Description("Path to a .sbv, .srt, .ass or .ssa file in your project's folder.\nThese can be made with a tool like aegisub.")]
        [Configurable] public string SubtitlesPath = "lyrics.srt";
        [Configurable] public float SubtitleY = 400;

        [Group("Font")]
        [Description("The name of a system font, or the path to a font relative to your project's folder.\nIt is preferable to add fonts to the project folder and use their file name rather than installing fonts.")]
        [Configurable] public string FontName = "Verdana";
        [Description("A path inside your mapset's folder where lyrics images will be generated.")]
        [Configurable] public string SpritesPath = "sb/f";
        [Description("The Size of the font.\nIncreasing the font size creates larger images.")]
        [Configurable] public int FontSize = 26;
        [Description("The Scale of the font.\nIncreasing the font scale does not creates larger images, but the result may be blurrier.")]
        [Configurable] public float FontScale = 0.5f;
        [Configurable] public Color4 FontColor = Color4.White;
        [Configurable] public FontStyle FontStyle = FontStyle.Regular;
        
        [Group("Outline")]
        [Configurable] public int OutlineThickness = 3;
        [Configurable] public Color4 OutlineColor = new Color4(50, 50, 50, 200);

        [Group("Shadow")]
        [Configurable] public int ShadowThickness = 0;
        [Configurable] public Color4 ShadowColor = new Color4(0, 0, 0, 100);

        [Group("Glow")]
        [Configurable] public int GlowRadius = 0;
        [Configurable] public Color4 GlowColor = new Color4(255, 255, 255, 100);
        [Configurable] public bool GlowAdditive = true;

        [Group("Misc")]
        [Configurable] public bool TrimTransparency = true;
        [Configurable] public bool EffectsOnly = false;
        [Description("How much extra space is allocated around the text when generating it.\nShould be increased when characters look cut off.")]
        [Configurable] public Vector2 Padding = Vector2.Zero;
        [Configurable] public OsbOrigin Origin = OsbOrigin.Centre;

        protected override void Generate()
        {
            var font = LoadFont(SpritesPath, new FontDescription
            {
                FontPath = FontName,
                FontSize = FontSize,
                Color = FontColor,
                Padding = Padding,
                FontStyle = FontStyle,
                TrimTransparency = TrimTransparency,
                EffectsOnly = EffectsOnly
            },
            new FontGlow
            {
                Radius = GlowAdditive ? 0 : GlowRadius,
                Color = GlowColor
            },
            new FontOutline
            {
                Thickness = OutlineThickness,
                Color = OutlineColor
            },
            new FontShadow
            {
                Thickness = ShadowThickness,
                Color = ShadowColor
            });

            var subtitles = LoadSubtitles(SubtitlesPath);

            if (GlowRadius > 0 && GlowAdditive)
            {
                var glowFont = LoadFont(Path.Combine(SpritesPath, "glow"), new FontDescription
                {
                    FontPath = FontName,
                    FontSize = FontSize,
                    Color = FontColor,
                    Padding = Padding,
                    FontStyle = FontStyle,
                    TrimTransparency = TrimTransparency,
                    EffectsOnly = true
                },
                new FontGlow()
                {
                    Radius = GlowRadius,
                    Color = GlowColor
                });
                generateLyrics(glowFont, subtitles, "glow", true);
            }
            generateLyrics(font, subtitles, "", false);
        }
        void generateLyrics(FontGenerator font, SubtitleSet subtitles, string layerName, bool additive)
        {
            var regex = new Regex(@"({\\k(\d+)})?([^{]+)");

            var layer = GetLayer(layerName);
            foreach (var subtitleLine in subtitles.Lines)
            {
                var letterY = SubtitleY;
                foreach (var line in subtitleLine.Text.Split('\n'))
                {
                    var matches = regex.Matches(line);

                    var lineWidth = 0f;
                    var lineHeight = 0f;
                    foreach (Match match in matches)
                    {
                        var text = match.Groups[3].Value;
                        foreach (var letter in text)
                        {
                            var texture = font.GetTexture(letter.ToString());
                            lineWidth += texture.BaseWidth * FontScale;
                            lineHeight = Math.Max(lineHeight, texture.BaseHeight * FontScale);
                        }
                    }

                    var karaokeStartTime = subtitleLine.StartTime;
                    var letterX = 320 - lineWidth * .5f;
                    foreach (Match match in matches)
                    {
                        var durationString = match.Groups[2].Value;
                        var duration = string.IsNullOrEmpty(durationString) ? subtitleLine.EndTime - subtitleLine.StartTime : int.Parse(durationString) * 10;
                        var karaokeEndTime = karaokeStartTime + duration;

                        var text = match.Groups[3].Value;

                        foreach (var letter in text)
                        {
                            var texture = font.GetTexture(letter.ToString());
                            if (!texture.IsEmpty)
                            {
                                var position = new Vector2(letterX, letterY) + texture.OffsetFor(Origin) * FontScale;

                                var sprite = layer.CreateSprite(texture.Path, Origin, position);
                                sprite.Scale(subtitleLine.StartTime, FontScale);
                                sprite.Fade(subtitleLine.StartTime - 200, subtitleLine.StartTime, 0, 1);
                                sprite.Fade(subtitleLine.EndTime - 200, subtitleLine.EndTime, 1, 0);
                                if (additive) sprite.Additive(subtitleLine.StartTime - 200, subtitleLine.EndTime);

                                applyKaraoke(sprite, subtitleLine, karaokeStartTime, karaokeEndTime);
                            }
                            letterX += texture.BaseWidth * FontScale;
                        }
                        karaokeStartTime += duration;
                    }
                    letterY += lineHeight;
                }
            }
        }
        void applyKaraoke(OsbSprite sprite, SubtitleLine subtitleLine, double startTime, double endTime)
        {
            var before = new Color4(.2f, .2f, .2f, 1f);
            var after = new Color4(.6f, .6f, .6f, 1f);

            sprite.Color(startTime - 100, startTime, before, Color.White);
            sprite.Color(endTime - 100, endTime, Color.White, after);
        }
    }
}