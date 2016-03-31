using Newtonsoft.Json.Linq;
using System.Text;

namespace StorybrewEditor.Util
{
    public static class JObjectExtensions
    {
        public static JObject ToJObject(this byte[] jsonBytes) 
            => JObject.Parse(Encoding.UTF8.GetString(jsonBytes).StripUtf8Bom());

        public static string GetName(this JToken token)
            => ((JProperty)token).Name;
    }
}
