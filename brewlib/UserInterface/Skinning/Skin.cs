using BrewLib.Graphics.Drawables;
using BrewLib.Graphics.Textures;
using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using Newtonsoft.Json.Linq;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;

namespace BrewLib.UserInterface.Skinning
{
    public class Skin : IDisposable
    {
        public readonly TextureContainer TextureContainer;
        public Func<string, Type> ResolveDrawableType;
        public Func<string, Type> ResolveWidgetType;
        public Func<string, Type> ResolveStyleType;

        private Dictionary<string, Drawable> drawables = new Dictionary<string, Drawable>();
        private Dictionary<Type, Dictionary<string, WidgetStyle>> stylesPerType = new Dictionary<Type, Dictionary<string, WidgetStyle>>();

        public Skin(TextureContainer textureContainer)
        {
            TextureContainer = textureContainer;
        }

        public Drawable GetDrawable(string name)
        {
            Drawable drawable;
            if (drawables.TryGetValue(name, out drawable))
                return drawable;

            return NullDrawable.Instance;
        }

        public T GetStyle<T>(string name) where T : WidgetStyle
            => (T)GetStyle(typeof(T), name);

        public WidgetStyle GetStyle(Type type, string name)
        {
            if (name == null) name = "default";

            Dictionary<string, WidgetStyle> styles;
            if (!stylesPerType.TryGetValue(type, out styles))
                return null;

            var n = name;
            WidgetStyle style;
            while (n != null)
            {
                if (styles.TryGetValue(n, out style))
                    return style;

                n = getImplicitParentStyleName(n);
            }

            if (getBaseStyleName(name) != "default")
            {
                var flags = getStyleFlags(name);
                if (flags != null) return GetStyle(type, $"default {flags}");
                else return GetStyle(type, "default");
            }

            return null;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var entry in drawables)
                        entry.Value.Dispose();
                    drawables.Clear();
                }
                drawables = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Loading

        public void Load(string filename, ResourceManager resourceManager = null)
            => Load(loadJson(filename, resourceManager), resourceManager);

        public void Load(JObject data, ResourceManager resourceManager = null)
        {
            //File.WriteAllText("_skin_debug.json", data.ToString());
            loadDrawables(data["drawables"]);
            loadStyles(data["styles"]);
        }

        private JObject loadJson(string filename, ResourceManager resourceManager)
        {
            byte[] data;
            if (File.Exists(filename))
                data = File.ReadAllBytes(filename);
            else
            {
                filename = filename.Substring(0, filename.LastIndexOf(".")).Replace('-', '_');
                data = resourceManager?.GetObject(filename) as byte[];
            }
            if (data == null) throw new FileNotFoundException(filename);
            return resolveIncludes(data.ToJObject(), resourceManager);
        }

        private JObject resolveIncludes(JObject data, ResourceManager resourceManager)
        {
            if (data["include"] != null)
            {
                var snapshot = new List<JToken>(data["include"]);
                foreach (var include in snapshot)
                {
                    var path = include.Value<string>();
                    var includedData = loadJson(path, resourceManager);
                    data.Merge(includedData, new JsonMergeSettings()
                    {
                        MergeArrayHandling = MergeArrayHandling.Union,
                        MergeNullValueHandling = MergeNullValueHandling.Merge,
                    });
                }
            }
            return data;
        }

        private void loadDrawables(JToken data)
        {
            if (data == null) return;

            foreach (var drawableData in data)
            {
                var drawableName = drawableData.GetName();
                try
                {
                    var drawable = loadDrawable(drawableData.First);
                    drawables.Add(drawableName, drawable);
                }
                catch (TypeLoadException)
                {
                    Trace.WriteLine($"Skin - Drawable type for {drawableName} doesn't exist");
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Skin - Failed to load drawable {drawableName}: {e}");
                }
            }
        }

        private Drawable loadDrawable(JToken data)
        {
            if (data.Type == JTokenType.String)
            {
                var value = data.Value<string>();
                if (string.IsNullOrEmpty(value))
                    return NullDrawable.Instance;

                var drawable = GetDrawable(value);
                if (drawable == NullDrawable.Instance)
                    throw new InvalidDataException($"Referenced drawable '{value}' must be defined before '{data.Path}'");
                return drawable;
            }
            else if (data.Type == JTokenType.Array)
            {
                var composite = new CompositeDrawable();
                foreach (var arrayDrawableData in data)
                {
                    var drawable = loadDrawable(arrayDrawableData);
                    composite.Drawables.Add(drawable);
                }
                return composite;
            }
            else
            {
                var drawableTypeData = data["_type"];
                if (drawableTypeData == null)
                    throw new InvalidDataException($"Drawable '{data.Path}' must declare a type");

                var drawableTypeName = drawableTypeData.Value<string>();
                var drawableType = ResolveDrawableType(drawableTypeName);
                var drawable = (Drawable)Activator.CreateInstance(drawableType);

                parseFields(drawable, data, null);

                return drawable;
            }
        }

        private void loadStyles(JToken data)
        {
            if (data == null) return;

            foreach (var styleTypeData in data)
            {
                var styleTypeName = styleTypeData.GetName();
                try
                {
                    var widgetType = ResolveWidgetType(styleTypeName);
                    var styleType = ResolveStyleType($"{styleTypeName}Style");

                    Dictionary<string, WidgetStyle> styles;
                    if (!stylesPerType.TryGetValue(styleType, out styles))
                        stylesPerType.Add(styleType, styles = new Dictionary<string, WidgetStyle>());

                    WidgetStyle defaultStyle = null;
                    foreach (var styleData in styleTypeData.First)
                    {
                        var styleName = styleData.GetName();
                        try
                        {
                            var style = (WidgetStyle)Activator.CreateInstance(styleType);

                            var parentStyle = defaultStyle;
                            var implicitParentStyleName = getImplicitParentStyleName(styleName);
                            if (implicitParentStyleName != null)
                            {
                                if (!styles.TryGetValue(implicitParentStyleName, out parentStyle) && styleTypeData.First[implicitParentStyleName] != null)
                                    throw new InvalidDataException($"Implicit parent style '{implicitParentStyleName}' style must be defined before '{styleName}'");

                                parentStyle = GetStyle(styleType, implicitParentStyleName);
                            }

                            var parentData = styleData.First["_parent"];
                            //if (implicitParentStyleName != null && parentData != null)
                            //    throw new InvalidDataException($"Style '{styleName}' is implicitely parented to '{implicitParentStyleName}' and must not declare a parent");
                            if (parentData != null && !styles.TryGetValue(parentData.Value<string>(), out parentStyle))
                                throw new InvalidDataException($"Parent style '{parentData.Value<string>()}' style must be defined before '{styleName}'");

                            parseFields(style, styleData.First, parentStyle);

                            if (defaultStyle == null)
                                if (styleName == "default") defaultStyle = style;
                                else throw new InvalidDataException($"The default {styleTypeName} style must be defined first");

                            styles.Add(styleName, style);
                        }
                        catch (InvalidDataException e)
                        {
                            Trace.WriteLine($"Skin - Invalid style {styleTypeName}.'{styleName}': {e.Message}");
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine($"Skin - Failed to load style {styleTypeName}.'{styleName}': {e}");
                        }
                    }
                }
                catch (TypeLoadException)
                {
                    Trace.WriteLine($"Skin - Widget type {styleTypeName} doesn't exist or isn't skinnable");
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Skin - Failed to load {styleTypeName} styles: {e}");
                }
            }
        }

        private void parseFields(object skinnable, JToken data, object parent)
        {
            var type = skinnable.GetType();
            while (type != typeof(object))
            {
                var fields = type.GetFields();
                foreach (var field in fields)
                {
                    var fieldData = resolveConstants(data[field.Name]);
                    if (fieldData != null)
                    {
                        var fieldType = field.FieldType;
                        var parser = getFieldParser(fieldType);
                        if (parser != null)
                        {
                            var value = parser.Invoke(fieldData, this);
                            field.SetValue(skinnable, value);
                        }
                        else Trace.WriteLine($"Skin - No parser for {fieldType}");
                    }
                    else if (parent != null)
                        field.SetValue(skinnable, field.GetValue(parent));
                }
                type = type.BaseType;
            }
        }

        private static JToken resolveConstants(JToken fieldData)
        {
            var constants = fieldData?.Root["constants"];
            while (fieldData != null && fieldData.Type == JTokenType.String)
            {
                var fieldString = fieldData.Value<string>();
                if (fieldString.StartsWith("@"))
                {
                    fieldData = constants?[fieldString.Substring(1)];
                    if (fieldData == null) throw new InvalidDataException($"Missing skin constant: {fieldString}");
                }
                else break;
            }
            return fieldData;
        }

        private static T resolve<T>(JToken data)
            => resolveConstants(data).Value<T>();

        private static string getBaseStyleName(string styleName)
        {
            var index = styleName.IndexOf(' ');
            if (index == -1) return styleName;
            return styleName.Substring(0, index);
        }

        private static string getStyleFlags(string styleName)
        {
            var index = styleName.LastIndexOf(' ');
            if (index == -1) return null;
            return styleName.Substring(index + 1, styleName.Length - index - 1);
        }

        private static string getImplicitParentStyleName(string styleName)
        {
            var index = styleName.LastIndexOf(' ');
            if (index == -1) return null;
            return styleName.Substring(0, index);
        }

        private static Func<JToken, Skin, object> getFieldParser(Type fieldType)
        {
            if (fieldType.IsEnum)
                return (data, skin) => Enum.Parse(fieldType, data.Value<string>());

            Func<JToken, Skin, object> parser;
            while (fieldType != typeof(object))
            {
                if (fieldParsers.TryGetValue(fieldType, out parser))
                    return parser;

                fieldType = fieldType.BaseType;
            }
            return null;
        }

        private static System.Drawing.ColorConverter colorConverter = new System.Drawing.ColorConverter();
        private static Dictionary<Type, Func<JToken, Skin, object>> fieldParsers = new Dictionary<Type, Func<JToken, Skin, object>>()
        {
            [typeof(string)] = (data, skin) => data.Value<string>(),
            [typeof(float)] = (data, skin) => data.Value<float>(),
            [typeof(double)] = (data, skin) => data.Value<double>(),
            [typeof(int)] = (data, skin) => data.Value<int>(),
            [typeof(bool)] = (data, skin) => data.Value<bool>(),
            [typeof(Texture2d)] = (data, skin) => skin.TextureContainer.Get(data.Value<string>()),
            [typeof(Drawable)] = (data, skin) => skin.loadDrawable(data),
            [typeof(Vector2)] = (data, skin) =>
            {
                if (data.Type == JTokenType.Array)
                    return new Vector2(resolve<float>(data[0]), resolve<float>(data[1]));
                throw new InvalidDataException($"Incorrect vector2 format: {data}");
            },
            [typeof(Color4)] = (data, skin) =>
            {
                if (data.Type == JTokenType.String)
                {
                    var value = data.Value<string>();
                    if (value.StartsWith("#"))
                    {
                        var color = (System.Drawing.Color)colorConverter.ConvertFromString(value);
                        return new Color4(color.R, color.G, color.B, color.A);
                    }

                    var colorMethod = typeof(Color4).GetMethod($"get_{value}");
                    if (colorMethod?.ReturnType == typeof(Color4))
                        return colorMethod.Invoke(null, null);
                }

                if (data.Type == JTokenType.Array)
                    switch (((JArray)data).Count)
                    {
                        case 3: return new Color4(resolve<float>(data[0]), resolve<float>(data[1]), resolve<float>(data[2]), 1f);
                        default:
                        case 4: return new Color4(resolve<float>(data[0]), resolve<float>(data[1]), resolve<float>(data[2]), resolve<float>(data[3]));
                    }
                throw new InvalidDataException($"Incorrect color format: {data}");
            },
            [typeof(FourSide)] = (data, skin) =>
            {
                if (data.Type == JTokenType.Array)
                    switch (((JArray)data).Count)
                    {
                        case 1: return new FourSide(resolve<float>(data[0]));
                        case 2: return new FourSide(resolve<float>(data[0]), resolve<float>(data[1]));
                        case 3: return new FourSide(resolve<float>(data[0]), resolve<float>(data[1]), resolve<float>(data[2]));
                        default:
                        case 4: return new FourSide(resolve<float>(data[0]), resolve<float>(data[1]), resolve<float>(data[2]), resolve<float>(data[3]));
                    }
                throw new InvalidDataException($"Incorrect four side format: {data}");
            },
        };

        #endregion
    }
}
