using System.Windows;
using System.Windows.Media.TextFormatting;

namespace StorybrewCommon.Subtitles.Internal
{
    internal class CustomTextParagraphProperties : TextParagraphProperties
    {
        public override FlowDirection FlowDirection => FlowDirection.LeftToRight;
        public override TextAlignment TextAlignment => TextAlignment.Left;
        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override double LineHeight => 96;
        public override bool FirstLineInParagraph => true;
        public override double Indent => 0;

        private readonly CustomTextRunProperties textRunProperties;
        public override TextRunProperties DefaultTextRunProperties => textRunProperties;

        public override TextMarkerProperties TextMarkerProperties => null;

        public CustomTextParagraphProperties(CustomTextRunProperties textRunProperties)
        {
            this.textRunProperties = textRunProperties;
        }
    }
}
