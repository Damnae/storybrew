using System.Globalization;

namespace StorybrewCommon.Storyboarding
{
    public class ExportSettings
    {
        public static ExportSettings Default = new ExportSettings();

        /// <summary>
        /// Not compatible with Fallback!
        /// </summary>
        public bool UseFloatForMove = true;

        public readonly NumberFormatInfo NumberFormat = new CultureInfo(@"en-US", false).NumberFormat;
    }
}
