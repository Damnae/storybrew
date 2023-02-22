using BrewLib.Util;
using System.Collections.Generic;
using System.IO;

namespace BrewLib.Data
{
    public class SubResourceContainer : ResourceContainer
    {
        readonly ResourceContainer resourceContainer;
        readonly string path;

        public IEnumerable<string> ResourceNames => resourceContainer.ResourceNames;

        public SubResourceContainer(ResourceContainer resourceContainer, string path)
        {
            this.resourceContainer = resourceContainer;
            this.path = path;
        }

        public Stream GetStream(string filename, ResourceSource sources)
            => resourceContainer.GetStream(applyPath(filename), sources) ?? resourceContainer.GetStream(filename, sources);

        public byte[] GetBytes(string filename, ResourceSource sources)
            => resourceContainer.GetBytes(applyPath(filename), sources) ?? resourceContainer.GetBytes(filename, sources);

        public string GetString(string filename, ResourceSource sources)
            => resourceContainer.GetString(applyPath(filename), sources) ?? resourceContainer.GetString(filename, sources);

        public SafeWriteStream GetWriteStream(string filename)
            => resourceContainer.GetWriteStream(applyPath(filename));

        string applyPath(string filename)
            => Path.Combine(path, filename);
    }
}