using StorybrewCommon.Util;
using StorybrewEditor.Util;
using System;
using System.IO;

namespace StorybrewEditor
{
    public class Settings
    {
        public readonly Setting<float> Volume = new Setting<float>(0.1f);
        public readonly Setting<bool> ShowStats = new Setting<bool>(false);

        private const string settingsFilename = "settings.cfg";

        public Settings()
        {
            if (!File.Exists(settingsFilename)) return;

            using (var stream = new FileStream(settingsFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                var type = GetType();
                reader.ParseKeyValueSection((key, value) =>
                {
                    var field = type.GetField(key);
                    if (field == null || !typeof(Setting).IsAssignableFrom(field.FieldType.GetGenericTypeDefinition()))
                        return;

                    var setting = (Setting)field.GetValue(this);
                    setting.Set(value);
                });
            }
        }

        public void Save()
        {
            using (var stream = new SafeWriteStream(settingsFilename))
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
            {
                foreach (var field in GetType().GetFields())
                {
                    if (!typeof(Setting).IsAssignableFrom(field.FieldType.GetGenericTypeDefinition()))
                        continue;

                    var setting = (Setting)field.GetValue(this);
                    writer.WriteLine($"{field.Name}: {setting}");
                }
            }
        }
    }

    public interface Setting
    {
        void Set(object value);
    }

    public class Setting<T> : Setting
    {
        private T value;

        public event EventHandler OnValueChanged;

        public Setting(T defaultValue)
        {
            value = defaultValue;
        }

        public void Set(T value)
        {
            if (this.value.Equals(value)) return;
            this.value = value;
            OnValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Set(object value)
        {
            this.value = (T)Convert.ChangeType(value, typeof(T));
        }

        public static implicit operator T(Setting<T> setting)
            => setting.value;

        public override string ToString()
            => value.ToString();
    }
}
