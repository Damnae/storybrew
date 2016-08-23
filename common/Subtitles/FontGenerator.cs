using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Threading;

namespace StorybrewCommon.Subtitles
{
    public class FontTexture
    {
        private string path;
        public string Path => path;
        public bool IsEmpty => path == null;

        private int baseWidth;
        public int BaseWidth => baseWidth;

        private int baseHeight;
        public int BaseHeight => baseHeight;

        private int width;
        public int Width => width;

        private int height;
        public int Height => height;

        public FontTexture(string path, int baseWidth, int baseHeight, int width, int height)
        {
            this.path = path;
            this.baseWidth = baseWidth;
            this.baseHeight = baseHeight;
            this.width = width;
            this.height = height;
        }
    }

    public class FontDescription
    {
        public string FontPath;
        public int FontSize = 76;
        public Color4 Color = new Color4(0, 0, 0, 100);
        public Vector2 Padding = Vector2.Zero;
        public bool Debug;
    }

    public class FontGenerator
    {
        private FontDescription description;
        private FontEffect[] effects;
        private string projectDirectory;
        private string mapsetDirectory;
        private string directory;

        private Dictionary<string, FontTexture> letters = new Dictionary<string, FontTexture>();

        public FontGenerator(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string mapsetDirectory)
        {
            this.description = description;
            this.effects = effects;
            this.projectDirectory = projectDirectory;
            this.mapsetDirectory = mapsetDirectory;
            this.directory = directory;
        }

        public FontTexture GetTexture(string text)
        {
            FontTexture texture;
            if (!letters.TryGetValue(text, out texture))
                letters.Add(text, texture = generateTexture(text));
            return texture;
        }

        private FontTexture generateTexture(string text)
        {
            var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{letters.Count:x3}.png";
            var bitmapPath = Path.Combine(mapsetDirectory, directory, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(bitmapPath));

            var fontPath = Path.Combine(projectDirectory, description.FontPath);
            if (!File.Exists(fontPath)) fontPath = description.FontPath;

            int baseWidth, baseHeight, width, height;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            using (StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic))
            using (var textBrush = new SolidBrush(Color.FromArgb(description.Color.ToArgb())))
            using (var fontCollection = new PrivateFontCollection())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;

                fontCollection.AddFontFile(fontPath);
                var fontFamily = fontCollection.Families[0];

                var fontStyle = FontStyle.Regular;
                using (var font = new Font(fontFamily, description.FontSize, fontStyle))
                {
                    var measuredSize = graphics.MeasureString(text, font, 0, stringFormat);
                    width = baseWidth = (int)(measuredSize.Width + 1 + description.Padding.X * 2);
                    height = baseHeight = (int)(measuredSize.Height + 1 + description.Padding.Y * 2);
                    foreach (var effect in effects)
                    {
                        var effectSize = effect.Measure();
                        width = Math.Max(width, (int)(baseWidth + effectSize.X));
                        height = Math.Max(height, (int)(baseHeight + effectSize.Y));
                    }

                    if (text.Length == 1 && char.IsWhiteSpace(text[0]))
                        return new FontTexture(null, baseWidth, baseHeight, width, height);

                    var offsetX = width / 2;
                    var offsetY = description.Padding.Y + (height - baseHeight) / 2;

                    using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics textGraphics = Graphics.FromImage(bitmap))
                        {
                            textGraphics.TextRenderingHint = graphics.TextRenderingHint;
                            textGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            textGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                            var r = new Random(letters.Count);
                            if (description.Debug)
                                textGraphics.Clear(Color.FromArgb(r.Next(100, 255), r.Next(100, 255), r.Next(100, 255)));

                            foreach (var effect in effects)
                                if (!effect.Overlay)
                                    effect.Draw(textGraphics, font, stringFormat, text, offsetX, offsetY);
                            textGraphics.DrawString(text, font, textBrush, offsetX, offsetY, stringFormat);
                            foreach (var effect in effects)
                                if (effect.Overlay)
                                    effect.Draw(textGraphics, font, stringFormat, text, offsetX, offsetY);
                            
                            if (description.Debug)
                                using (var pen = new Pen(Color.FromArgb(255, 0, 0)))
                                    textGraphics.DrawLine(pen, offsetX, offsetY, offsetX, offsetY + baseHeight);
                        }
                        withRetries(() => bitmap.Save(bitmapPath, ImageFormat.Png));
                    }
                }
            }
            return new FontTexture(Path.Combine(directory, filename), baseWidth, baseHeight, width, height);
        }

        private static void withRetries(Action action, int timeout = 2000)
        {
            var sleepTime = 0;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch
                {
                    if (sleepTime >= timeout) throw;

                    var retryDelay = timeout / 10;
                    sleepTime += retryDelay;
                    Thread.Sleep(retryDelay);
                }
            }
        }
    }
}