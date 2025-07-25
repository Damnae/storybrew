﻿using BrewLib.Util;
using StorybrewCommon.Animations;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Subtitles;
using StorybrewCommon.Subtitles.Parsers;
using StorybrewCommon.Util;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tiny;

namespace StorybrewCommon.Scripting
{
    public abstract class StoryboardObjectGenerator : Script
    {
        [ThreadStatic]
        public static StoryboardObjectGenerator Current;

        private List<ConfigurableField> configurableFields;
        private GeneratorContext context;

        /// <summary>
        /// Override to true if this script uses multiple threads.
        /// It will prevent other effects from updating in parallel to this one.
        /// </summary>
        public virtual bool Multithreaded => false;

        /// <summary>
        /// Place <c>CancellationToken.ThrowIfCancellationRequested();</c> in long loops of your script, to allow interrupting it from the editor.
        /// CancellationToken must also be passed to Parallel loops to allow their cancellation.
        /// </summary>
        public CancellationToken CancellationToken => context.CancellationToken;

        /// <summary>
        /// Creates or retrieves a layer. 
        /// The identifier will be shown in the editor as "Effect name (Identifier)". 
        /// Layers will be sorted by the order in which they are first retrieved.
        /// </summary>
        public StoryboardLayer GetLayer(string identifier) => context.GetLayer(identifier);

        public Beatmap Beatmap => context.Beatmap;
        public Beatmap GetBeatmap(string name)
            => context.Beatmaps.FirstOrDefault(b => b.Name == name);

        public string ProjectPath => context.ProjectPath;
        public string AssetPath => context.ProjectAssetPath;
        public string MapsetPath => context.MapsetPath;

        public StoryboardObjectGenerator()
        {
            initializeConfigurableFields();
        }

        public void AddDependency(string path)
            => context.AddDependency(path);

        public void Log(string message)
            => context.AppendLog(message);

        public void Log(object message)
            => Log(message.ToString());

        public void Assert(bool condition, string message = null, [CallerLineNumber] int line = -1)
        {
            if (!condition)
                throw new Exception(message != null ? $"Assertion failed line {line}: {message}" : $"Assertion failed line {line}");
        }

        #region File loading

        private readonly Dictionary<string, Bitmap> bitmaps = new Dictionary<string, Bitmap>();

        /// <summary>
        /// Returns a Bitmap from the project's directory.
        /// Do not call Dispose, it will be disposed automatically when the script ends.
        /// </summary>
        public Bitmap GetProjectBitmap(string path, bool watch = true)
            => getBitmap(Path.Combine(context.ProjectPath, path), null, watch);

        /// <summary>
        /// Returns a Bitmap from the mapset's directory.
        /// Do not call Dispose, it will be disposed automatically when the script ends.
        /// </summary>
        public Bitmap GetMapsetBitmap(string path, bool watch = true)
            => getBitmap(Path.Combine(context.MapsetPath, path), Path.Combine(context.ProjectAssetPath, path), watch);

        private Bitmap getBitmap(string path, string alternatePath, bool watch)
        {
            path = Path.GetFullPath(path);

            if (!bitmaps.TryGetValue(path, out Bitmap bitmap))
            {
                if (watch) context.AddDependency(path);

                if (alternatePath != null && !File.Exists(path))
                {
                    alternatePath = Path.GetFullPath(alternatePath);
                    if (watch) context.AddDependency(alternatePath);

                    try
                    {
                        bitmaps.Add(path, bitmap = BrewLib.Util.Misc.WithRetries(() => (Bitmap)Image.FromFile(alternatePath)));
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new FileNotFoundException(path, e);
                    }
                }
                else bitmaps.Add(path, bitmap = BrewLib.Util.Misc.WithRetries(() => (Bitmap)Image.FromFile(path)));
            }
            return bitmap;
        }

        /// <summary>
        /// Opens a project file in read-only mode. 
        /// You are responsible for disposing it.
        /// </summary>
        public Stream OpenProjectFile(string path, bool watch = true)
            => openFile(Path.Combine(context.ProjectPath, path), watch);

        /// <summary>
        /// Opens a mapset file in read-only mode. 
        /// You are responsible for disposing it.
        /// </summary>
        public Stream OpenMapsetFile(string path, bool watch = true)
            => openFile(Path.Combine(context.MapsetPath, path), watch);

        private Stream openFile(string path, bool watch)
        {
            path = Path.GetFullPath(path);
            if (watch) context.AddDependency(path);
            return BrewLib.Util.Misc.WithRetries(() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        #endregion

        #region Random

        [Group("Common")]
        [Description("Changes the result of Random(...) calls.")]
        [Configurable] public int RandomSeed;

        private Random random;
        public int Random(int minValue, int maxValue) => random.Next(minValue, maxValue);
        public int Random(int maxValue) => random.Next(maxValue);
        public double Random(double minValue, double maxValue) => minValue + random.NextDouble() * (maxValue - minValue);
        public double Random(double maxValue) => random.NextDouble() * maxValue;
        public float Random(float minValue, float maxValue) => minValue + (float)random.NextDouble() * (maxValue - minValue);
        public float Random(float maxValue) => (float)random.NextDouble() * maxValue;

        #endregion

        #region Audio data

        public double AudioDuration => context.AudioDuration;

        /// <summary>
        /// Returns the Fast Fourier Transform of the song at a certain time, with the default amount of magnitudes.
        /// Useful to make spectrum effets.
        /// </summary>
        public float[] GetFft(double time, string path = null, bool splitChannels = false)
        {
            if (path != null) AddDependency(path);
            return context.GetFft(time, path, splitChannels);
        }

        /// <summary>
        /// Returns the Fast Fourier Transform of the song at a certain time, with the specified amount of magnitudes.
        /// Useful to make spectrum effets.
        /// </summary>
        public float[] GetFft(double time, int magnitudes, string path = null, OsbEasing easing = OsbEasing.None, float frequencyCutOff = 0)
        {
            var fft = GetFft(time, path);
            if (magnitudes == fft.Length && easing == OsbEasing.None)
                return fft;

            var usedFftLength = frequencyCutOff > 0 ?
                (int)Math.Floor(frequencyCutOff / (context.GetFftFrequency(path) / 2.0) * fft.Length) :
                fft.Length;

            var resultFft = new float[magnitudes];
            var baseIndex = 0;
            for (var i = 0; i < magnitudes; i++)
            {
                var progress = EasingFunctions.Ease(easing, (double)i / magnitudes);
                var index = Math.Min(Math.Max(baseIndex + 1, (int)(progress * usedFftLength)), usedFftLength - 1);

                var value = 0f;
                for (var v = baseIndex; v < index; v++)
                    value = Math.Max(value, fft[index]);

                resultFft[i] = value;
                baseIndex = index;
            }
            return resultFft;
        }

        #endregion

        #region Subtitles

        private readonly SrtParser srtParser = new SrtParser();
        private readonly AssParser assParser = new AssParser();
        private readonly SbvParser sbvParser = new SbvParser();

        private readonly HashSet<string> fontDirectories = new HashSet<string>();
        private readonly List<FontGenerator> fontGenerators = new List<FontGenerator>();

        private string fontCacheDirectory => Path.Combine(context.ProjectPath, ".cache", "font");

        public SubtitleSet LoadSubtitles(string path)
        {
            path = Path.Combine(context.ProjectPath, path);
            context.AddDependency(path);

            switch (Path.GetExtension(path))
            {
                case ".srt": return srtParser.Parse(path);
                case ".ssa":
                case ".ass": return assParser.Parse(path);
                case ".sbv": return sbvParser.Parse(path);
            }
            throw new NotSupportedException($"{Path.GetExtension(path)} isn't a supported subtitle format");
        }

        public FontGenerator LoadFont(string directory, FontDescription description, params FontEffect[] effects)
            => LoadFont(directory, false, description, effects);

        public FontGenerator LoadFont(string directory, bool asAsset, FontDescription description, params FontEffect[] effects)
        {
            var assetDirectory = asAsset ? context.ProjectAssetPath : context.MapsetPath;

            var fontDirectory = Path.GetFullPath(Path.Combine(assetDirectory, directory));
            if (fontDirectories.Contains(fontDirectory))
                throw new InvalidOperationException($"This effect already generated a font inside \"{fontDirectory}\"");
            fontDirectories.Add(fontDirectory);

            var fontGenerator = new FontGenerator(directory, description, effects, context.ProjectPath, assetDirectory);
            fontGenerators.Add(fontGenerator);

            var cachePath = fontCacheDirectory;
            if (Directory.Exists(cachePath))
            {
                var path = Path.Combine(cachePath, HashHelper.GetMd5(fontGenerator.Directory) + ".yaml");
                if (File.Exists(path))
                {
                    var cachedFontRoot = Util.Misc.WithRetries(() => TinyToken.Read(path), canThrow: false);
                    if (cachedFontRoot != null)
                        fontGenerator.HandleCache(cachedFontRoot);
                }
            }

            return fontGenerator;
        }

        private void saveFontCache()
        {
            var cachePath = fontCacheDirectory;
            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);

            foreach (var fontGenerator in fontGenerators)
            {
                var path = Path.Combine(cachePath, HashHelper.GetMd5(fontGenerator.Directory) + ".yaml");

                var fontRoot = fontGenerator.ToTinyObject();
                try
                {
                    fontRoot.Write(path);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Failed to save font cache for {path} ({e.GetType().FullName})");
                }
            }
        }

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
                            Value = Convert.ChangeType(value, fieldType, CultureInfo.InvariantCulture),
                        };
                    }
                }

                try
                {
                    var displayName = configurableField.Attribute.DisplayName;
                    var initialValue = Convert.ChangeType(configurableField.InitialValue, fieldType, CultureInfo.InvariantCulture);
                    config.UpdateField(field.Name, displayName, configurableField.Description, configurableField.Order, fieldType, initialValue, allowedValues, configurableField.BeginsGroup);

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
                var configurable = field.GetCustomAttribute<ConfigurableAttribute>(true);
                if (configurable == null)
                    continue;

                if (!field.FieldType.IsEnum && !ObjectSerializer.Supports(field.FieldType.FullName))
                    continue;

                var group = field.GetCustomAttribute<GroupAttribute>(true);
                var description = field.GetCustomAttribute<DescriptionAttribute>(true);

                configurableFields.Add(new ConfigurableField()
                {
                    Field = field,
                    Attribute = configurable,
                    InitialValue = field.GetValue(this),
                    BeginsGroup = group?.Name?.Trim(),
                    Description = description?.Content?.Trim(),
                    Order = order++,
                });
            }
        }

        private struct ConfigurableField
        {
            public FieldInfo Field;
            public ConfigurableAttribute Attribute;
            public object InitialValue;
            public string BeginsGroup;
            public string Description;
            public int Order;

            public override string ToString() => $"{Field.Name} {InitialValue}";
        }

        #endregion

        public void Generate(GeneratorContext context)
        {
            if (Current != null) throw new InvalidOperationException("A script is already running in this domain");

            try
            {
                this.context = context;

                random = new Random(RandomSeed);

                Current = this;
                Generate();

                context.Multithreaded = Multithreaded;
                saveFontCache();
            }
            finally
            {
                this.context = null;
                Current = null;

                foreach (var bitmap in bitmaps.Values)
                    bitmap.Dispose();
                bitmaps.Clear();
            }
        }

        public abstract void Generate();
    }
}
