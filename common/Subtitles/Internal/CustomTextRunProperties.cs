using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace StorybrewCommon.Subtitles.Internal
{
    internal class CustomTextRunProperties : TextRunProperties
    {
        private readonly Typeface typeface;
        public override Typeface Typeface => typeface;
        public override CultureInfo CultureInfo => CultureInfo.CurrentCulture;
        public override TextDecorationCollection TextDecorations => null;
        public override TextEffectCollection TextEffects => null;

        public override Brush ForegroundBrush => Brushes.White;
        public override Brush BackgroundBrush => Brushes.Transparent;

        private readonly double fontSize;
        public override double FontRenderingEmSize => fontSize;
        public override double FontHintingEmSize => fontSize;

        public override BaselineAlignment BaselineAlignment { get; }
        private readonly TextRunTypographyProperties typographyProperties = new CustomTextRunTypographyProperties();
        public override TextRunTypographyProperties TypographyProperties => typographyProperties;
        public override NumberSubstitution NumberSubstitution { get; }

        public CustomTextRunProperties(Typeface typeface, double fontSize)
        {
            this.typeface = typeface;
            this.fontSize = fontSize;
        }
    }
}
