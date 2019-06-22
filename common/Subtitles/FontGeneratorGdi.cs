using StorybrewCommon.Util;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace StorybrewCommon.Subtitles
{
    public class FontGeneratorGdi : FontGenerator
    {
        public FontGeneratorGdi(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string mapsetDirectory)
            : base(directory, description, effects, projectDirectory, mapsetDirectory)
        {
        }

        protected override FontTexture GenerateTexture(string text, string fontPath, string bitmapPath)
        {
            float offsetX = 0, offsetY = 0;
            int baseWidth, baseHeight, width, height;
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            using (var stringFormat = new StringFormat(StringFormat.GenericTypographic))
            using (var textBrush = new SolidBrush(Color.FromArgb(Description.Color.ToArgb())))
            using (var fontCollection = new PrivateFontCollection())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;

                FontFamily fontFamily = null;
                if (File.Exists(fontPath))
                {
                    fontCollection.AddFontFile(fontPath);
                    fontFamily = fontCollection.Families[0];
                }

                var dpiScale = 96f / graphics.DpiY;
                var fontStyle = Description.FontStyle;
                using (var font = fontFamily != null ? new Font(fontFamily, Description.FontSize * dpiScale, fontStyle) : new Font(fontPath, Description.FontSize * dpiScale, fontStyle))
                {
                    var measuredSize = graphics.MeasureString(text, font, 0, stringFormat);
                    baseWidth = (int)Math.Ceiling(measuredSize.Width);
                    baseHeight = (int)Math.Ceiling(measuredSize.Height);

                    var effectsWidth = 0f;
                    var effectsHeight = 0f;
                    foreach (var effect in Effects)
                    {
                        var effectSize = effect.Measure();
                        effectsWidth = Math.Max(effectsWidth, effectSize.X);
                        effectsHeight = Math.Max(effectsHeight, effectSize.Y);
                    }
                    width = (int)Math.Ceiling(baseWidth + effectsWidth + Description.Padding.X * 2);
                    height = (int)Math.Ceiling(baseHeight + effectsHeight + Description.Padding.Y * 2);

                    var paddingX = Description.Padding.X + effectsWidth * 0.5f;
                    var paddingY = Description.Padding.Y + effectsHeight * 0.5f;
                    var textX = paddingX + measuredSize.Width * 0.5f;
                    var textY = paddingY;

                    offsetX = -paddingX;
                    offsetY = -paddingY;

                    if (text.Length == 1 && char.IsWhiteSpace(text[0]) || width == 0 || height == 0)
                        return new FontTexture(null, offsetX, offsetY, baseWidth, baseHeight, width, height);

                    using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        using (var textGraphics = Graphics.FromImage(bitmap))
                        {
                            textGraphics.TextRenderingHint = graphics.TextRenderingHint;
                            textGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            textGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                            if (Description.Debug)
                            {
                                var r = new Random(TextureCacheSize);
                                textGraphics.Clear(Color.FromArgb(r.Next(100, 255), r.Next(100, 255), r.Next(100, 255)));
                            }

                            foreach (var effect in Effects)
                                if (!effect.Overlay)
                                    effect.Draw(bitmap, textGraphics, font, stringFormat, text, textX, textY);
                            if (!Description.EffectsOnly)
                                textGraphics.DrawString(text, font, textBrush, textX, textY, stringFormat);
                            foreach (var effect in Effects)
                                if (effect.Overlay)
                                    effect.Draw(bitmap, textGraphics, font, stringFormat, text, textX, textY);

                            if (Description.Debug)
                                using (var pen = new Pen(Color.FromArgb(255, 0, 0)))
                                {
                                    textGraphics.DrawLine(pen, textX, textY, textX, textY + baseHeight);
                                    textGraphics.DrawLine(pen, textX - baseWidth * 0.5f, textY, textX + baseWidth * 0.5f, textY);
                                }
                        }

                        var absoluteBitmapPath = Path.Combine(MapsetDirectory, bitmapPath);

                        var bounds = Description.TrimTransparency ? BitmapHelper.FindTransparencyBounds(bitmap) : null;
                        if (bounds != null && bounds != new Rectangle(0, 0, bitmap.Width, bitmap.Height))
                        {
                            var trimBounds = bounds.Value;
                            using (var trimmedBitmap = new Bitmap(trimBounds.Width, trimBounds.Height))
                            {
                                offsetX += trimBounds.Left;
                                offsetY += trimBounds.Top;
                                width = trimmedBitmap.Width;
                                height = trimmedBitmap.Height;
                                using (var trimGraphics = Graphics.FromImage(trimmedBitmap))
                                    trimGraphics.DrawImage(bitmap, 0, 0, trimBounds, GraphicsUnit.Pixel);
                                BrewLib.Util.Misc.WithRetries(() => trimmedBitmap.Save(absoluteBitmapPath, ImageFormat.Png));
                            }
                        }
                        else BrewLib.Util.Misc.WithRetries(() => bitmap.Save(absoluteBitmapPath, ImageFormat.Png));
                    }
                }
            }
            return new FontTexture(bitmapPath, offsetX, offsetY, baseWidth, baseHeight, width, height);
        }
    }
}