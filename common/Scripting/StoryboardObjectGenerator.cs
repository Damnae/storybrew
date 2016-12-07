using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Subtitles;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace StorybrewCommon.Scripting
{
    public abstract class StoryboardObjectGenerator : Script
    {
        private static StoryboardObjectGenerator current;
        public static StoryboardObjectGenerator Current => current;

        private List<ConfigurableField> configurableFields;
        private GeneratorContext context;

        /// <summary>
        /// Creates or retrieves a layer. 
        /// The identifier will be shown in the editor as "Effect name (Identifier)". 
        /// Layers will be sorted by the order in which they are first retrieved.
        /// </summary>
        protected StoryboardLayer GetLayer(string identifier) => context.GetLayer(identifier);

        protected Beatmap Beatmap => context.Beatmap;
        protected string MapsetPath => context.MapsetPath;
        protected string ProjectPath => context.ProjectPath;

        public StoryboardObjectGenerator()
        {
            initializeConfigurableFields();
        }

        public void AddDependency(string path)
            => context.AddDependency(path);

        public void Log(string message)
            => context.AppendLog(message);

        public void Assert(bool condition, string message = null, [CallerLineNumber] int line = -1)
        {
            if (!condition)
                throw new Exception(message != null ? $"Assertion failed line {line}: {message}" : $"Assertion failed line {line}");
        }

        #region File loading

        private Dictionary<string, Bitmap> bitmaps = new Dictionary<string, Bitmap>();

        /// <summary>
        /// Returns a Bitmap from the project's directory.
        /// Do not call Dispose, it will be disposed automatically when the script ends.
        /// </summary>
        public Bitmap GetProjectBitmap(string path)
            => getBitmap(Path.Combine(context.ProjectPath, path));

        /// <summary>
        /// Returns a Bitmap from the mapset's directory.
        /// Do not call Dispose, it will be disposed automatically when the script ends.
        /// </summary>
        public Bitmap GetMapsetBitmap(string path)
            => getBitmap(Path.Combine(context.MapsetPath, path));

        private Bitmap getBitmap(string path)
        {
            path = Path.GetFullPath(path);

            Bitmap bitmap;
            if (!bitmaps.TryGetValue(path, out bitmap))
            {
                context.AddDependency(path);
                bitmaps.Add(path, bitmap = Misc.WithRetries(() => (Bitmap)Image.FromFile(path)));
            }
            return bitmap;
        }

        /// <summary>
        /// Opens a project file in read-only mode. 
        /// You are responsible for disposing it.
        /// </summary>
        public Stream OpenProjectFile(string path)
            => openFile(Path.Combine(context.ProjectPath, path));

        /// <summary>
        /// Opens a mapset file in read-only mode. 
        /// You are responsible for disposing it.
        /// </summary>
        public Stream OpenMapsetFile(string path)
            => openFile(Path.Combine(context.MapsetPath, path));

        private Stream openFile(string path)
        {
            path = Path.GetFullPath(path);
            context.AddDependency(path);
            return Misc.WithRetries(() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        #endregion

        #region Random

        [Configurable(DisplayName = "Random seed")]
        public int RandomSeed;

        private Random random;
        public int Random(int minValue, int maxValue) => random.Next(minValue, maxValue);
        public int Random(int maxValue) => random.Next(maxValue);
        public double Random(double minValue, double maxValue) => minValue + random.NextDouble() * (maxValue - minValue);
        public double Random(double maxValue) => random.NextDouble() * maxValue;

        #endregion

        #region Audio data

        public double AudioDuration => context.AudioDuration;

        /// <summary>
        /// Returns the Fast Fourier Transform of the song at a certain time, with the default amount of magnitudes.
        /// Useful to make spectrum effets.
        /// </summary>
        public float[] GetFft(double time, string path = null)
        {
            if (path != null) AddDependency(path);
            return context.GetFft(time, path);
        }

        /// <summary>
        /// Returns the Fast Fourier Transform of the song at a certain time, with the specified amount of magnitudes.
        /// Useful to make spectrum effets.
        /// </summary>
        public float[] GetFft(double time, int magnitudes, string path = null)
        {
            var fft = GetFft(time, path);
            var resultFft = new float[magnitudes];

            if (magnitudes == fft.Length)
                Array.Copy(fft, resultFft, magnitudes);
            else
                for (var i = 0; i < magnitudes; i++)
                {
                    var left = (int)(((double)i / magnitudes) * fft.Length);
                    var right = (int)(((i + 1.0) / magnitudes) * fft.Length);

                    if (left == right)
                        right++;

                    var value = 0f;
                    for (var j = left; j < right; j++)
                        value = Math.Max(value, fft[j]);

                    resultFft[i] = value;
                }
            return resultFft;
        }

        #endregion

        #region Subtitles

        private SrtParser srtParser = new SrtParser();
        private AssParser assParser = new AssParser();

        public SubtitleSet LoadSubtitles(string path)
        {
            path = Path.Combine(context.ProjectPath, path);
            context.AddDependency(path);

            switch (Path.GetExtension(path))
            {
                case ".srt": return srtParser.Parse(path);
                case ".ssa":
                case ".ass": return assParser.Parse(path);
            }
            throw new NotSupportedException($"{Path.GetExtension(path)} isn't a supported subtitle format");
        }

        public FontGenerator LoadFont(string directory, FontDescription description, params FontEffect[] effects)
            => new FontGenerator(directory, description, effects, context.ProjectPath, context.MapsetPath);

        #endregion

        #region Configuration

        public void UpdateConfiguration(EffectConfig config)
        {
            if (context != null) throw new InvalidOperationException();

            var remainingFieldNames = new List<string>(config.FieldNames);
            foreach (var configurableField in configurableFields)
            {
                var field = configurableField.Field;
                var allowedValues = (NamedValue[])null;

                var fieldType = field.FieldType;
                if (fieldType.IsEnum)
                {
                    var enumValues = Enum.GetValues(fieldType);
                    fieldType = Enum.GetUnderlyingType(fieldType);

                    allowedValues = new NamedValue[enumValues.Length];
                    for (var i = 0; i < enumValues.Length; i++)
                    {
                        var value = enumValues.GetValue(i);
                        allowedValues[i] = new NamedValue()
                        {
                            Name = value.ToString(),
                            Value = Convert.ChangeType(value, fieldType),
                        };
                    }
                }

                try
                {
                    var displayName = configurableField.Attribute.DisplayName ?? field.Name;
                    var initialValue = Convert.ChangeType(configurableField.InitialValue, fieldType);
                    config.UpdateField(field.Name, displayName, configurableField.Order, fieldType, initialValue, allowedValues);

                    var value = config.GetValue(field.Name);
                    field.SetValue(this, value);

                    remainingFieldNames.Remove(field.Name);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Failed to update configuration for {field.Name} with type {fieldType}:\n{e}");
                }
            }
            foreach (var name in remainingFieldNames)
                config.RemoveField(name);
        }

        public void ApplyConfiguration(EffectConfig config)
        {
            if (context != null) throw new InvalidOperationException();

            foreach (var configurableField in configurableFields)
            {
                var field = configurableField.Field;
                try
                {
                    var value = config.GetValue(field.Name);
                    field.SetValue(this, value);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Failed to apply configuration for {field.Name}:\n{e}");
                }
            }
        }

        private void initializeConfigurableFields()
        {
            configurableFields = new List<ConfigurableField>();

            var order = 0;
            var type = GetType();
            foreach (var field in type.GetFields())
            {
                foreach (var attribute in field.GetCustomAttributes(true))
                {
                    var configurable = attribute as ConfigurableAttribute;
                    if (configurable == null) continue;

                    if (!field.FieldType.IsEnum && !ObjectSerializer.Supports(field.FieldType.FullName))
                        continue;

                    configurableFields.Add(new ConfigurableField()
                    {
                        Field = field,
                        Attribute = configurable,
                        InitialValue = field.GetValue(this),
                        Order = order++,
                    });
                    break;
                }
            }
        }

        private struct ConfigurableField
        {
            public FieldInfo Field;
            public ConfigurableAttribute Attribute;
            public object InitialValue;
            public int Order;

            public override string ToString() => $"{Field.Name} {InitialValue}";
        }

        #endregion

        public void Generate(GeneratorContext context)
        {
            if (current != null) throw new InvalidOperationException();
            try
            {
                this.context = context;

                random = new Random(RandomSeed);

                current = this;
                Generate();
                current = null;
            }
            finally
            {
                this.context = null;

                foreach (var bitmap in bitmaps.Values)
                    bitmap.Dispose();
                bitmaps.Clear();
            }
        }

        public abstract void Generate();
    }
}
