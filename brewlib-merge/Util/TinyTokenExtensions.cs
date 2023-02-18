using System.IO;
using Tiny;

namespace BrewLib.Util
{
    public static class TinyTokenExtensions
    {
        public static void Merge(this TinyToken into, TinyToken token)
        {
            if (token is TinyObject tinyObject && into is TinyObject intoObject) foreach (var entry in tinyObject)
                {
                    var existing = intoObject.Value<TinyToken>(entry.Key);
                    if (existing != null) existing.Merge(entry.Value);
                    else intoObject.Add(entry);
                }
            else if (token is TinyArray tinyArray && into is TinyArray intoArray) foreach (var entry in tinyArray) intoArray.Add(entry);
            else throw new InvalidDataException($"Cannot merge {token} into {into}");
        }
    }
}