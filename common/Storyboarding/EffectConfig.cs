using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewCommon.Storyboarding
{
    public class EffectConfig : MarshalByRefObject
    {
        private Dictionary<string, ConfigField> fields = new Dictionary<string, ConfigField>();

        public int FieldCount => fields.Count;
        public IEnumerable<ConfigField> Fields => fields.Values;
        public IEnumerable<ConfigField> SortedFields
        {
            get
            {
                var sortedValues = new List<ConfigField>(fields.Values);
                sortedValues.Sort((first, second) => first.Order - second.Order);
                return sortedValues;
            }
        }

        public string[] FieldNames
        {
            get
            {
                var names = new string[fields.Keys.Count];
                fields.Keys.CopyTo(names, 0);
                return names;
            }
        }

        public void UpdateField(string name, string displayName, int order, Type fieldType, object defaultValue, NamedValue[] allowedValues)
        {
            if (fieldType == null)
                return;

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
                Order = order,
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
                Order = field.Order,
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
                return Convert.ChangeType(value, newType, CultureInfo.InvariantCulture);
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
            public int Order;
        }
    }
}
