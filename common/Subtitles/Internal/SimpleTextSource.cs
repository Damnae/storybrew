using System;
using System.Globalization;
using System.Windows.Media.TextFormatting;

namespace StorybrewCommon.Subtitles.Internal
{
    internal class SimpleTextSource : TextSource
    {
        private readonly string text;
        private readonly TextRunProperties textRunProperties;

        public SimpleTextSource(string text, TextRunProperties textRunProperties)
        {
            this.text = text;
            this.textRunProperties = textRunProperties;
        }

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            var characterBufferRange = new CharacterBufferRange(text, 0, textSourceCharacterIndexLimit);
            return new TextSpan<CultureSpecificCharacterBufferRange>(textSourceCharacterIndexLimit, 
                new CultureSpecificCharacterBufferRange(CultureInfo.CurrentCulture, characterBufferRange));
        }

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new NotImplementedException();
        }

        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            if (textSourceCharacterIndex < 0)
                throw new ArgumentOutOfRangeException("textSourceCharacterIndex", "Value must be greater than 0.");

            if (textSourceCharacterIndex >= text.Length)
                return new TextEndOfParagraph(1);

            if (textSourceCharacterIndex < text.Length)
                return new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, textRunProperties);
            
            return new TextEndOfParagraph(1);
        }
    }
}