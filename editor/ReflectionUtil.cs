using System.Reflection;

namespace StorybrewEditor
{
    public static class ReflectionUtil
    {
        private static string _version;

        public static string GetVersionInfo()
        {
            if (_version != null) return _version;

            var assembly = Assembly.GetEntryAssembly();
            var ver = ((AssemblyInformationalVersionAttribute)assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute), false)[0]).InformationalVersion;

            _version = ver;
            return _version;
        }
    }
}