using BrewLib.Util;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace BrewLib.Data
{
    public class ResourceContainer
    {
        private readonly Assembly assembly;
        private readonly string basePath;

        public ResourceContainer(Assembly assembly, string basePath)
        {
            this.assembly = assembly;
            this.basePath = basePath;

            foreach (var name in assembly.GetManifestResourceNames())
                Debug.Print(name);
        }

        public Stream GetStream(string filename, bool allowFiles = false)
        {
            if (allowFiles && File.Exists(filename))
                return new FileStream(filename, FileMode.Open);

            var resourceName = $"{basePath}.{filename.Replace('\\', '.').Replace('/', '.')}";
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) Trace.WriteLine($"Resource '{filename}' / '{resourceName}' not found");
            return stream;
        }

        public byte[] GetBytes(string filename, bool allowFiles = false)
        {
            using (var stream = GetStream(filename, allowFiles))
            {
                if (stream == null)
                    return null;

                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public string GetString(string filename, bool allowFiles = false)
            => Encoding.UTF8.GetString(GetBytes(filename)).StripUtf8Bom();
    }
}
