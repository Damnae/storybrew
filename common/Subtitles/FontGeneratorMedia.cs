using StorybrewCommon.Subtitles.Internal;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;

namespace StorybrewCommon.Subtitles
{
    public class FontGeneratorMedia : FontGenerator
    {
        public FontGeneratorMedia(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string mapsetDirectory)
            : base(directory, description, effects, projectDirectory, mapsetDirectory)
        {
        }

        protected override FontTexture GenerateTexture(string text, string fontPath, string bitmapPath)
        {
            var visual = new DrawingVisual();
            using (var drawingContext = visual.RenderOpen())
            {
                var fontFamily = File.Exists(fontPath) ?
                    new FontFamily(new Uri(Path.GetDirectoryName(fontPath)), Path.GetFileNameWithoutExtension(fontPath)) :
                    new FontFamily(fontPath);
                var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Regular, FontStretches.Normal);

                var textRunProperties = new CustomTextRunProperties(typeface, Description.FontSize);
                var paragraphProperties = new CustomTextParagraphProperties(textRunProperties);

                var textSource = new SimpleTextSource(text, textRunProperties);
                
                var formatter = TextFormatter.Create();
                using (var textLine = formatter.FormatLine(textSource, 0, 0, paragraphProperties, null))
                    textLine.Draw(drawingContext, new Point(), InvertAxes.None);
            }

            var bounds = visual.ContentBounds;
            if (bounds.IsEmpty)
                return new FontTexture(null, 0, 0, 0, 0, 0, 0);

            var width = (int)Math.Ceiling(bounds.Width);
            var height = (int)Math.Ceiling(bounds.Height);

            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);
            renderTarget.Render(visual);

            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(renderTarget));

            var absoluteBitmapPath = Path.Combine(MapsetDirectory, bitmapPath);
            using (var stream = File.Create(absoluteBitmapPath))
                pngEncoder.Save(stream);

            return new FontTexture(bitmapPath, 0, 0, width, height, width, height);
        }
    }
}