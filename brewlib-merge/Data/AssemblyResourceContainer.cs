using BrewLib.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BrewLib.Data
{
    public class AssemblyResourceContainer : ResourceContainer
    {
        readonly Assembly assembly;
        readonly string baseNamespace, basePath;

        public IEnumerable<string> ResourceNames => assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith($"{baseNamespace}.")).Select(name => name.Substring(baseNamespace.Length + 1));

        public AssemblyResourceContainer(Assembly assembly = null, string baseNamespace = null, string basePath = null)
        {
            this.assembly = assembly ?? Assembly.GetEntryAssembly();
            this.baseNamespace = baseNamespace ?? $"{this.assembly.EntryPoint.DeclaringType.Namespace}.Resources";
            this.basePath = basePath ?? "resources";
        }
        public Stream GetStream(string path, ResourceSource sources)
        {
            if (path == null) return null;

            if (Path.IsPathRooted(path))
            {
                if (sources.HasFlag(ResourceSource.Absolute))
                {
                    if (File.Exists(path)) return new FileStream(path, FileMode.Open, FileAccess.Read);
                }
                else throw new InvalidOperationException($"Resource paths must be relative ({path})");
            }
            else
            {
                if (sources.HasFlag(ResourceSource.Relative))
                {
                    var combinedPath = basePath != null ? Path.Combine(basePath, path) : path;
                    if (File.Exists(combinedPath)) return new FileStream(combinedPath, FileMode.Open, FileAccess.Read);
                }
                if (sources.HasFlag(ResourceSource.Embedded))
                {
                    var resourceName = $"{baseNamespace}.{path.Replace('\\', '.').Replace('/', '.')}";
                    var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null) return stream;
                }
            }

            Trace.WriteLine($"Not found: {path} ({sources})", "Resources");
            return null;
        }
        public byte[] GetBytes(string path, ResourceSource sources = ResourceSource.Embedded)
        {
            using (var stream = GetStream(path, sources))
            {
                if (stream == null) return null;

                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }
        public string GetString(string path, ResourceSource sources = ResourceSource.Embedded)
        {
            var bytes = GetBytes(path, sources);
            return bytes != null ? Encoding.UTF8.GetString(bytes).StripUtf8Bom() : null;
        }
        public SafeWriteStream GetWriteStream(string path)
        {
            if (Path.IsPathRooted(path)) throw new InvalidOperationException($"Resource paths must be relative ({path})");

            var combinedPath = basePath != null ? Path.Combine(basePath, path) : path;
            return new SafeWriteStream(combinedPath);
        }
    }
}