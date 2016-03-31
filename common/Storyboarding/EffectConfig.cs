using StorybrewCommon.Util;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding
{
    public class EffectConfig : MarshalByRefObject
    {
        private Dictionary<string, ConfigField> fields = new Dictionary<string, ConfigField>();

        public string ConfigurationTarget;

        public IEnumerable<ConfigField> Fields => fields.Values;
        public int FieldCount => fields.Count;

        public string[] FieldNames
        {
            get
            {
                var names = new string[fields.Keys.Count];
                fields.Keys.CopyTo(names, 0);
                return names;
            }
        }

        public void UpdateField(string name, string displayName, Type fieldType, object defaultValue, NamedValue[] allowedValues)
        {
            ConfigField field;
            var value = fields.TryGetValue(name, out field) ?
                convertFieldValue(field.Value, field.Type, fieldType, defaultValue) :
                defaultValue;

            var isAllowed = allowedValues == null;
            if (!isAllowed)
                foreach (var allowedValue in allowedValues)
                    if (value.Equals(allowedValue.Value))
                    {
                        isAllowed = true;
                        break;
                    }
            if (!isAllowed)
                value = defaultValue;

            fields[name] = new ConfigField()
            {
                Name = name,
                DisplayName = displayName,
                Value = value,
                Type = fieldType,
                AllowedValues = allowedValues,
            };
        }

        public void RemoveField(string name)
            => fields.Remove(name);


        public bool SetValue(string name, object value)
        {
            var field = fields[name];
            if (field.Value == value)
                return false;

            fields[name] = new ConfigField()
            {
                Name = field.Name,
                DisplayName = field.DisplayName,
                Value = value,
                Type = field.Type,
                AllowedValues = field.AllowedValues,
            };
            return true;
        }

        public object GetValue(string name)
            => fields[name].Value;

        private object convertFieldValue(object value, Type oldType, Type newType, object defaultValue)
        {
            if (newType.IsAssignableFrom(oldType))
                return value;

            try
            {
                return Convert.ChangeType(value, newType);
            }
            catch
            {
                return defaultValue;
            }
        }

        public struct ConfigField
        {
            public string Name;
            public string DisplayName;
            public object Value;
            public Type Type;
            public NamedValue[] AllowedValues;
        }
    }
}
