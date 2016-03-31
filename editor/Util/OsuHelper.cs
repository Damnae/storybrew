using Microsoft.Win32;
using System.IO;

namespace StorybrewEditor.Util
{
    public static class OsuHelper
    {
        public static string GetOsuPath()
        {
            using (var registryKey = Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon"))
            {
                if (registryKey == null)
                    return string.Empty;

                var value = registryKey.GetValue(null).ToString();
                var startIndex = value.IndexOf("\"");
                var endIndex = value.LastIndexOf("\"");
                return value.Substring(startIndex + 1, endIndex - 1);
            }
        }

        public static string GetOsuFolder()
            => Path.GetDirectoryName(GetOsuPath());

        public static string GetOsuSongFolder()
        {
            var osuFolder = Path.GetDirectoryName(GetOsuPath());
            var songsFolder = Path.Combine(osuFolder, "Songs");
            return Directory.Exists(songsFolder) ? songsFolder : osuFolder;
        }
    }
}
