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
    public class FontText
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

        public FontText(string path, int baseWidth, int baseHeight, int width, int height)
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
        public bool Outlined;
        public int ShadowThickness = 8;
        public int HorizontalPadding = 0;
        public int VerticalPadding = 0;
        public Color TextColor = Color.FromArgb(255, 255, 255, 255);
        public Color ShadowColor = Color.FromArgb(100, 0, 0, 0);
        public bool Debug;
    }

    public class FontGenerator
    {
        private FontDescription description;
        private string projectDirectory;
        private string mapsetDirectory;
        private string directory;

        private Dictionary<string, FontText> letters = new Dictionary<string, FontText>();

        public FontGenerator(string directory, FontDescription description, string projectDirectory, string mapsetDirectory)
        {
            this.description = description;
            this.projectDirectory = projectDirectory;
            this.mapsetDirectory = mapsetDirectory;
            this.directory = directory;
        }

        public FontText GetText(string text)
        {
            FontText letter;
            if (!letters.TryGetValue(text, out letter))
                letters.Add(text, letter = generateText(text));
            return letter;
        }

        private FontText generateText(string text)
        {
            var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{letters.Count:x3}.png";
            var bitmapPath = Path.Combine(mapsetDirectory, directory, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(bitmapPath));

            var fontPath = Path.Combine(projectDirectory, description.FontPath);
            if (!File.Exists(fontPath)) fontPath = description.FontPath;

            int baseWidth, baseHeight, width, height;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            using (StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic))
            using (var textBrush = new SolidBrush(description.TextColor))
            using (var shadowBrush = new SolidBrush(description.ShadowColor))
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
                    var shadowExpandFactor = 1.4f;

                    var measuredSize = graphics.MeasureString(text, font, 0, stringFormat);
                    baseWidth = (int)measuredSize.Width + 1 + description.HorizontalPadding * 2;
                    baseHeight = (int)measuredSize.Height + 1 + description.VerticalPadding * 2;
                    width = (int)(baseWidth + description.ShadowThickness * shadowExpandFactor * 2);
                    height = (int)(baseHeight + description.ShadowThickness * shadowExpandFactor * 2);

                    if (text.Length == 1 && char.IsWhiteSpace(text[0]))
                        return new FontText(null, baseWidth, baseHeight, width, height);

                    var offsetX = width / 2;
                    var offsetY = description.VerticalPadding + description.ShadowThickness * shadowExpandFactor;

                    using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics textGraphics = Graphics.FromImage(bitmap))
                        {
                            textGraphics.TextRenderingHint = graphics.TextRenderingHint;
                            textGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            textGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                            if (description.Debug)
                            {
                                var r = new Random();
                                textGraphics.Clear(Color.FromArgb(r.Next(100, 255), r.Next(100, 255), r.Next(100, 255)));
                            }

                            for (var i = 1; i <= description.ShadowThickness; i++)
                            {
                                if (description.Outlined)
                                {
                                    if (i % 2 == 0)
                                    {
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX - i * shadowExpandFactor, offsetY, stringFormat);
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX, offsetY - i * shadowExpandFactor, stringFormat);
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX + i * shadowExpandFactor, offsetY, stringFormat);
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX, offsetY + i * shadowExpandFactor, stringFormat);
                                    }
                                    else
                                    {
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX - i, offsetY - i, stringFormat);
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX - i, offsetY + i, stringFormat);
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX + i, offsetY + i, stringFormat);
                                        textGraphics.DrawString(text, font, shadowBrush, offsetX + i, offsetY - i, stringFormat);
                                    }
                                }
                                else textGraphics.DrawString(text, font, shadowBrush, offsetX + i, offsetY + i, stringFormat);
                            }
                            textGraphics.DrawString(text, font, textBrush, offsetX, offsetY, stringFormat);
                        }
                        withRetries(() => bitmap.Save(bitmapPath, ImageFormat.Png));
                    }
                }
            }
            return new FontText(Path.Combine(directory, filename), baseWidth, baseHeight, width, height);
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