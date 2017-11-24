using System.Collections.Generic;
using System.IO;

namespace BrewLib.Data
{
    public class SubResourceContainer : ResourceContainer
    {
        private readonly ResourceContainer resourceContainer;
        private readonly string path;

        public IEnumerable<string> ResourceNames => resourceContainer.ResourceNames;

        public SubResourceContainer(ResourceContainer resourceContainer, string path)
        {
            this.resourceContainer = resourceContainer;
            this.path = path;
        }

        public Stream GetStream(string filename, ResourceSource sources = ResourceSource.Embedded)
            => resourceContainer.GetStream(applyPath(filename), sources) ??
            resourceContainer.GetStream(filename, sources);

        public byte[] GetBytes(string filename, ResourceSource sources = ResourceSource.Embedded)
            => resourceContainer.GetBytes(applyPath(filename), sources) ??
            resourceContainer.GetBytes(filename, sources);

        public string GetString(string filename, ResourceSource sources = ResourceSource.Embedded)
            => resourceContainer.GetString(applyPath(filename), sources) ??
            resourceContainer.GetString(filename, sources);

        private string applyPath(string filename)
            => Path.Combine(path, filename);
    }
}
