using System.Windows;
using System.Windows.Media.TextFormatting;

namespace StorybrewCommon.Subtitles.Internal
{
    internal class CustomTextRunTypographyProperties : TextRunTypographyProperties
    {
        public override bool StandardLigatures => true;
        public override bool ContextualLigatures => true;
        public override bool DiscretionaryLigatures => true;
        public override bool HistoricalLigatures => true;

        public override bool ContextualAlternates => true;
        public override bool HistoricalForms => true;
        public override bool Kerning => true;
        public override bool CapitalSpacing => true;
        public override bool CaseSensitiveForms => true;
        public override bool StylisticSet1 => true;
        public override bool StylisticSet2 => true;
        public override bool StylisticSet3 => true;
        public override bool StylisticSet4 => true;
        public override bool StylisticSet5 => true;
        public override bool StylisticSet6 => true;
        public override bool StylisticSet7 => true;
        public override bool StylisticSet8 => true;
        public override bool StylisticSet9 => true;
        public override bool StylisticSet10 => true;
        public override bool StylisticSet11 => true;
        public override bool StylisticSet12 => true;
        public override bool StylisticSet13 => true;
        public override bool StylisticSet14 => true;
        public override bool StylisticSet15 => true;
        public override bool StylisticSet16 => true;
        public override bool StylisticSet17 => true;
        public override bool StylisticSet18 => true;
        public override bool StylisticSet19 => true;
        public override bool StylisticSet20 => true;

        public override bool SlashedZero => true;
        public override bool MathematicalGreek => true;
        public override bool EastAsianExpertForms => true;

        public override FontVariants Variants => FontVariants.Normal;
        public override FontCapitals Capitals => FontCapitals.Normal;
        public override FontFraction Fraction => FontFraction.Normal;
        public override FontNumeralStyle NumeralStyle => FontNumeralStyle.Normal;
        public override FontNumeralAlignment NumeralAlignment => FontNumeralAlignment.Normal;
        public override FontEastAsianWidths EastAsianWidths => FontEastAsianWidths.Normal;
        public override FontEastAsianLanguage EastAsianLanguage => FontEastAsianLanguage.Normal;

        public override int StandardSwashes => 0;
        public override int ContextualSwashes => 0;
        public override int StylisticAlternates => 1;
        public override int AnnotationAlternates => 1;
    }
}
