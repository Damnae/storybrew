using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace StorybrewCommon.Scripting
{
    public abstract class StoryboardObjectGenerator : Script
    {
        private string identifier = Guid.NewGuid().ToString();
        private List<ConfigurableField> configurableFields;
        private GeneratorContext context;

        [Configurable(DisplayName = "Random seed")]
        public int RandomSeed;

        private Random random;
        public int Random(int minValue, int maxValue) => random.Next(minValue, maxValue);
        public int Random(int maxValue) => random.Next(maxValue);
        public double Random(double minValue, double maxValue) => minValue + random.NextDouble() * (maxValue - minValue);
        public double Random(double maxValue) => random.NextDouble() * maxValue;

        /// <summary>
        /// Creates or retrieves a layer. 
        /// The identifier will be shown in the editor as "Effect name (Identifier)". 
        /// Layers will be sorted by the order in which they are first retrieved.
        /// </summary>
        protected StoryboardLayer GetLayer(string identifier) => context.GetLayer(identifier);

        protected Beatmap Beatmap => context.Beatmap;

        public StoryboardObjectGenerator()
        {
            initializeConfigurableFields();
        }

        /// <summary>
        /// Returns the Fast Fourier Transform of the song at a certain time, with the default amount of magnitudes.
        /// Useful to make spectrum effets.
        /// </summary>
        public float[] GetFft(double time)
            => context.GetFft(time);

        /// <summary>
        /// Returns the Fast Fourier Transform of the song at a certain time, with the specified amount of magnitudes.
        /// Useful to make spectrum effets.
        /// </summary>
        public float[] GetFft(double time, int magnitudes)
        {
            var fft = GetFft(time);
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

        public bool Configure(EffectConfig config)
        {
            if (context != null) throw new InvalidOperationException();

            if (config.ConfigurationTarget != identifier)
            {
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
                        config.UpdateField(field.Name, displayName, fieldType, initialValue, allowedValues);

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

                config.ConfigurationTarget = identifier;
                return true;
            }
            else
            {
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
                return false;
            }
        }

        public void Generate(GeneratorContext context)
        {
            try
            {
                this.context = context;

                random = new Random(RandomSeed);
                Generate();
            }
            finally
            {
                this.context = null;
            }
        }

        public abstract void Generate();

        private void initializeConfigurableFields()
        {
            configurableFields = new List<ConfigurableField>();

            var type = GetType();
            foreach (var field in type.GetFields())
            {
                foreach (var attribute in field.GetCustomAttributes(true))
                {
                    var configurable = attribute as ConfigurableAttribute;
                    if (configurable == null) continue;

                    if (!ObjectSerializer.Supports(field.FieldType))
                        continue;

                    configurableFields.Add(new ConfigurableField()
                    {
                        Field = field,
                        Attribute = configurable,
                        InitialValue = field.GetValue(this),
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
        }
    }
}
