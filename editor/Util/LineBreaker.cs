using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StorybrewEditor.Util
{
    public class LineBreaker
    {
        public static List<string> Split(string text, float maxWidth, Func<char, int> measure)
        {
            var lines = new List<string>();
            var textArray = text.ToCharArray();

            int startIndex = 0, endIndex = 0, lineWidth = 0;
            Action completeLine = () =>
            {
                lineWidth = 0;

                var length = endIndex - startIndex + 1;
                Debug.Assert(length > 0);

                lines.Add(new string(textArray, startIndex, length));

                startIndex = endIndex + 1;
                endIndex = startIndex;
            };

            for (; endIndex < text.Length; endIndex++)
            {
                var character = text[endIndex];
                var characterWidth = measure(character);

                if (maxWidth > 0 && endIndex > startIndex && (lineWidth + characterWidth) > maxWidth)
                {
                    var firstAllowed = -1;
                    var breakIndex = endIndex;
                    while (true)
                    {
                        var breakability = getBreakability(text, breakIndex);
                        if (breakability == Breakability.Opportunity)
                        {
                            endIndex = breakIndex - 1;
                            break;
                        }
                        else if (breakability == Breakability.Allowed && firstAllowed == -1)
                            firstAllowed = breakIndex - 1;

                        breakIndex--;
                        if (breakIndex <= startIndex)
                        {
                            if (firstAllowed != -1)
                                endIndex = firstAllowed;
                            else
                                endIndex--;
                            break;
                        }
                    }
                    completeLine();
                }

                lineWidth += characterWidth;

                if (mustBreakAfter(text, endIndex))
                    completeLine();
            }
            if (lineWidth > 0)
                completeLine();

            return lines;
        }

        // A lazy implementation of http://unicode.org/reports/tr14/
        //
        // Classes not implemented: 
        // CM SG ZWJ HY CB and everything after

        private static int[] breakOpportunityAfter = new int[]
        {
            0x200B, // ZERO WIDTH SPACE
            0x0020, // SPACE
            0x2014, // EM DASH

            0x1680, // OGHAM SPACE MARK
            0x2000, // EN QUAD
            0x2001, // EM QUAD
            0x2002, // EN SPACE
            0x2003, // EM SPACE
            0x2004, // THREE-PER-EM SPACE
            0x2005, // FOUR-PER-EM SPACE
            0x2006, // SIX-PER-EM SPACE
            0x2008, // PUNCTUATION SPACE
            0x2009, // THIN SPACE
            0x200A, // HAIR SPACE
            0x205F, // MEDIUM MATHEMATICAL SPACE
            0x3000, // IDEOGRAPHIC SPACE
            0x0009, // TAB
            0x00AD, // SOFT HYPHEN
            0x058A, // ARMENIAN HYPHEN
            0x2010, // HYPHEN
            0x2012, // FIGURE DASH
            0x2013, // EN DASH
            0x05BE, // HEBREW PUNCTUATION MAQAF
            0x0F0B, // TIBETAN MARK INTERSYLLABIC TSHEG
            0x1361, // ETHIOPIC WORDSPACE
            0x17D8, // KHMER SIGN BEYYAL
            0x17DA, // KHMER SIGN KOOMUUT
            0x2027, // HYPHENATION POINT
            0x007C, // VERTICAL LINE
            0x16EB, // RUNIC SINGLE DOT PUNCTUATION
            0x16EC, // RUNIC MULTIPLE DOT PUNCTUATION
            0x16ED, // RUNIC CROSS PUNCTUATION
            0x2056, // THREE DOT PUNCTUATION
            0x2058, // FOUR DOT PUNCTUATION
            0x2059, // FIVE DOT PUNCTUATION
            0x205A, // TWO DOT PUNCTUATION
            0x205B, // FOUR DOT MARK
            0x205D, // TRICOLON
            0x205E, // VERTICAL FOUR DOTS
            0x2E19, // PALM BRANCH
            0x2E2A, // TWO DOTS OVER ONE DOT PUNCTUATION
            0x2E2B, // ONE DOT OVER TWO DOTS PUNCTUATION
            0x2E2C, // SQUARED FOUR DOT PUNCTUATION
            0x2E2D, // FIVE DOT PUNCTUATION
            0x2E30, // RING POINT
            0x10100, // AEGEAN WORD SEPARATOR LINE
            0x10101, // AEGEAN WORD SEPARATOR DOT
            0x10102, // AEGEAN CHECK MARK
            0x1039F, // UGARITIC WORD DIVIDER
            0x103D0, // OLD PERSIAN WORD DIVIDER
            0x1091F, // PHOENICIAN WORD DIVIDER
            0x12470, // CUNEIFORM PUNCTUATION SIGN OLD ASSYRIAN WORD DIVIDER
            0x0964, // DEVANAGARI DANDA
            0x0965, // DEVANAGARI DOUBLE DANDA
            0x0E5A, // THAI CHARACTER ANGKHANKHU
            0x0E5B, // THAI CHARACTER KHOMUT
            0x104A, // MYANMAR SIGN LITTLE SECTION
            0x104B, // MYANMAR SIGN SECTION
            0x1735, // PHILIPPINE SINGLE PUNCTUATION
            0x1736, // PHILIPPINE DOUBLE PUNCTUATION
            0x17D4, // KHMER SIGN KHAN
            0x17D5, // KHMER SIGN BARIYOOSAN
            0x1B5E, // BALINESE CARIK SIKI
            0x1B5F, // BALINESE CARIK PAREREN
            0xA8CE, // SAURASHTRA DANDA
            0xA8CF, // SAURASHTRA DOUBLE DANDA
            0xAA5D, // CHAM PUNCTUATION DANDA
            0xAA5E, // CHAM PUNCTUATION DOUBLE DANDA
            0xAA5F, // CHAM PUNCTUATION TRIPLE DANDA
            0x10A56, // KHAROSHTHI PUNCTUATION DANDA
            0x10A57, // KHAROSHTHI PUNCTUATION DOUBLE DANDA
            0x0F34, // TIBETAN MARK BSDUS RTAGS
            0x0F7F, // TIBETAN SIGN RNAM BCAD
            0x0F85, // TIBETAN MARK PALUTA
            0x0FBE, // TIBETAN KU RU KHA
            0x0FBF, // TIBETAN KU RU KHA BZHI MIG CAN
            0x0FD2, // TIBETAN MARK NYIS TSHEG
            0x1804, // MONGOLIAN COLON
            0x1805, // MONGOLIAN FOUR DOTS
            0x1B5A, // BALINESE PANTI
            0x1B5B, // BALINESE PAMADA
            0x1B5D, // BALINESE CARIK PAMUNGKAH
            0x1B60, // BALINESE PAMENENG
            0x1C3B, // LEPCHA PUNCTUATION TA-ROL
            0x1C3C, // LEPCHA PUNCTUATION NYET THYOOM TA-ROL
            0x1C3D, // LEPCHA PUNCTUATION CER-WA
            0x1C3E, // LEPCHA PUNCTUATION TSHOOK CER-WA
            0x1C3F, // LEPCHA PUNCTUATION TSHOOK
            0x1C7E, // OL CHIKI PUNCTUATION MUCAAD
            0x1C7F, // OL CHIKI PUNCTUATION DOUBLE MUCAAD
            0x2CFA, // COPTIC OLD NUBIAN DIRECT QUESTION MARK
            0x2CFB, // COPTIC OLD NUBIAN INDIRECT QUESTION MARK
            0x2CFC, // COPTIC OLD NUBIAN VERSE DIVIDER
            0x2CFF, // COPTIC MORPHOLOGICAL DIVIDER
            0x2E17, // OBLIQUE DOUBLE HYPHEN
            0xA60D, // VAI COMMA
            0xA60F, // VAI QUESTION MARK
            0xA92E, // KAYAH LI SIGN CWI
            0xA92F, // KAYAH LI SIGN SHYA
            0x10A50, // KHAROSHTHI PUNCTUATION DOT
            0x10A51, // KHAROSHTHI PUNCTUATION SMALL CIRCLE
            0x10A52, // KHAROSHTHI PUNCTUATION CIRCLE
            0x10A53, // KHAROSHTHI PUNCTUATION CRESCENT BAR
            0x10A54, // KHAROSHTHI PUNCTUATION MANGALAM
            0x10A55, // KHAROSHTHI PUNCTUATION LOTUS
        };
        private static int[] breakOpportunityBefore = new int[]
        {
            0x200B, // ZERO WIDTH SPACE
            0x0020, // SPACE
            0x2014, // EM DASH

            0x00B4, // ACUTE ACCENT
            0x1FFD, // GREEK OXIA
            0x02DF, // MODIFIER LETTER CROSS ACCENT
            0x02C8, // MODIFIER LETTER VERTICAL LINE
            0x02CC, // MODIFIER LETTER LOW VERTICAL LINE
            0x0F01, // TIBETAN MARK GTER YIG MGO TRUNCATED A
            0x0F02, // TIBETAN MARK GTER YIG MGO -UM RNAM BCAD MA
            0x0F03, // TIBETAN MARK GTER YIG MGO -UM GTER TSHEG MA
            0x0F04, // TIBETAN MARK INITIAL YIG MGO MDUN MA
            0x0F06, // TIBETAN MARK CARET YIG MGO PHUR SHAD MA
            0x0F07, // TIBETAN MARK YIG MGO TSHEG SHAD MA
            0x0F09, // TIBETAN MARK BSKUR YIG MGO
            0x0F0A, // TIBETAN MARK BKA- SHOG YIG MGO
            0x0FD0, // TIBETAN MARK BSKA- SHOG GI MGO RGYAN
            0x0FD1, // TIBETAN MARK MNYAM YIG GI MGO RGYAN
            0x0FD3, // TIBETAN MARK INITIAL BRDA RNYING YIG MGO MDUN MA
            0xA874, // PHAGS-PA SINGLE HEAD MARK
            0xA875, // PHAGS-PA DOUBLE HEAD MARK
            0x1806, // MONGOLIAN TODO SOFT HYPHEN
        };
        private static int[] breakProhibitedAfter = new int[]
        {
            0x2060, // WORD JOINER
            0xFEFF, // ZERO WIDTH NO-BREAK SPACE
            0x00A0, // NO-BREAK SPACE
            0x202F, // NARROW NO-BREAK SPACE
            0x180E, // MONGOLIAN VOWEL SEPARATOR
            0x034F, // COMBINING GRAPHEME JOINER
            0x2007, // FIGURE SPACE
            0x2011, // NON-BREAKING HYPHEN (NBHY)
            0x0F08, // TIBETAN MARK SBRUL SHAD
            0x0F0C, // TIBETAN MARK DELIMITER TSHEG BSTAR
            0x0F12, // TIBETAN MARK RGYA GRAM SHAD
        };
        private static int[] breakProhibitedBefore = new int[]
        {
            0x2060, // WORD JOINER
            0xFEFF, // ZERO WIDTH NO-BREAK SPACE
            0x00A0, // NO-BREAK SPACE
            0x202F, // NARROW NO-BREAK SPACE
            0x180E, // MONGOLIAN VOWEL SEPARATOR
            0x034F, // COMBINING GRAPHEME JOINER
            0x2007, // FIGURE SPACE
            0x2011, // NON-BREAKING HYPHEN (NBHY)
            0x0F08, // TIBETAN MARK SBRUL SHAD
            0x0F0C, // TIBETAN MARK DELIMITER TSHEG BSTAR
            0x0F12, // TIBETAN MARK RGYA GRAM SHAD
        };
        private static int[] causesBreakAfter = new int[]
        {
            0x000C, // FORM FEED
            0x000B, // LINE TABULATION
            0x2028, // LINE SEPARATOR
            0x2029, // PARAGRAPH SEPARATOR
            0x000A, // LINE FEED
            0x0085, // NEXT LINE
        };

        private static Breakability getBreakability(string text, int index)
        {
            if (index == 0) return Breakability.Prohibited;

            var after = getBreakabilityAfter(text[index - 1]);
            var before = getBreakabilityBefore(text[index]);

            if (after == Breakability.Prohibited || before == Breakability.Prohibited)
                return Breakability.Prohibited;

            if (after == Breakability.Opportunity || before == Breakability.Opportunity)
                return Breakability.Opportunity;

            return Breakability.Allowed;
        }

        private static Breakability getBreakabilityAfter(char c)
        {
            if (breakOpportunityAfter.Contains(c))
                return Breakability.Opportunity;

            if (c >= 0x2E0E && c <= 0x2E15) // EDITORIAL CORONIS..UPWARDS ANCORA
                return Breakability.Opportunity;

            if (breakProhibitedAfter.Contains(c))
                return Breakability.Prohibited;

            if (c >= 0x035C && c <= 0x0362) // COMBINING DOUBLE BREVE BELOW..COMBINING DOUBLE RIGHTWARDS ARROW BELOW
                return Breakability.Prohibited;

            return Breakability.Allowed;
        }

        private static Breakability getBreakabilityBefore(char c)
        {
            if (breakOpportunityBefore.Contains(c))
                return Breakability.Opportunity;

            if (breakProhibitedBefore.Contains(c))
                return Breakability.Prohibited;

            if (c >= 0x035C && c <= 0x0362) // COMBINING DOUBLE BREVE BELOW..COMBINING DOUBLE RIGHTWARDS ARROW BELOW
                return Breakability.Prohibited;

            return Breakability.Allowed;
        }

        private static bool mustBreakAfter(string text, int index)
        {
            if (index == text.Length - 1) return true;

            var c = text[index];

            if (causesBreakAfter.Contains(c))
                return true;

            if (c == 0x000D && text[index + 1] != 0x000A) // CARRIAGE RETURN, except before a LINE FEED
                return true;

            return false;
        }

        public enum Breakability
        {
            Opportunity,
            Allowed,
            Prohibited,
        }
    }
}
