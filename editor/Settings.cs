using BrewLib.UserInterface;
using BrewLib.Util;
using StorybrewCommon.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace StorybrewEditor
{
    public class Settings
    {
        public const string DefaultPath = "settings.cfg";

        public readonly Setting<string> Id = new Setting<string>(Guid.NewGuid().ToString("N")), TimeCopyFormat = new Setting<string>(@"h\:mm\:ss\.ff");
        public readonly Setting<int> FrameRate = new Setting<int>(0), UpdateRate = new Setting<int>(60), EffectThreads = new Setting<int>(0);
        public readonly Setting<float> Volume = new Setting<float>(.5f);
        public readonly Setting<bool> FitStoryboard = new Setting<bool>(false), ShowStats = new Setting<bool>(false),
            VerboseVsCode = new Setting<bool>(false), UseRoslyn = new Setting<bool>(false);

        readonly string path;

        public Settings(string path = DefaultPath)
        {
            this.path = path;

            if (!File.Exists(path))
            {
                Save();
                return;
            }

            Trace.WriteLine($"Loading settings from '{path}'");

            var type = GetType();
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                reader.ParseKeyValueSection((key, value) =>
                {
                    var field = type.GetField(key);
                    if (field == null || !field.FieldType.IsGenericType || !typeof(Setting).IsAssignableFrom(field.FieldType.GetGenericTypeDefinition()))
                        return;

                    try
                    {
                        var setting = (Setting)field.GetValue(this);
                        setting.Set(value);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Failed to load setting {key} with value {value}: {e}");
                    }
                });
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to load settings: {e}");
                Save();
            }
        }
        public void Save()
        {
            Trace.WriteLine($"Saving settings at '{path}'");

            using (var stream = new SafeWriteStream(path)) using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
            {
                foreach (var field in GetType().GetFields())
                {
                    if (!field.FieldType.IsGenericType || !typeof(Setting).IsAssignableFrom(field.FieldType.GetGenericTypeDefinition()))
                        continue;

                    var setting = (Setting)field.GetValue(this);
                    writer.WriteLine($"{field.Name}: {setting}");
                }
                stream.Commit();
            }
        }
    }
    public interface Setting
    {
        void Set(object value);
    }
    public class Setting<T> : Setting
    {
        T value;
        public event EventHandler OnValueChanged;

        public Setting(T defaultValue) => value = defaultValue;

        public void Set(T value)
        {
            if (this.value.Equals(value)) return;
            this.value = value;
            OnValueChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Set(object value) => Set((T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture));

        public void Bind(Field field, Action changedAction)
        {
            field.OnValueChanged += (sender, e) => Set(field.FieldValue);
            EventHandler handler;
            OnValueChanged += handler = (sender, e) =>
            {
                field.FieldValue = value;
                changedAction();
            };
            field.OnDisposed += (sender, e) => OnValueChanged -= handler;
            handler(this, EventArgs.Empty);
        }

        public static implicit operator T(Setting<T> setting) => setting.value;

        public override string ToString()
        {
            if (typeof(T).GetInterface(nameof(IConvertible)) != null) return Convert.ToString(value, CultureInfo.InvariantCulture);
            return value.ToString();
        }
    }
    public static class SettingsExtensions
    {
        public static void BindToSetting<T>(this Button button, Setting<T> setting, Action changedAction)
        {
            button.OnValueChanged += (sender, e) => setting.Set(button.Checked);
            EventHandler handler;
            setting.OnValueChanged += handler = (sender, e) =>
            {
                button.Checked = (bool)Convert.ChangeType((T)setting, typeof(bool));
                changedAction();
            };
            button.OnDisposed += (sender, e) => setting.OnValueChanged -= handler;
            handler(button, EventArgs.Empty);
        }
    }
}