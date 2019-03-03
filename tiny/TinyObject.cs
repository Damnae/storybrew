using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Tiny
{
    public class TinyObject : TinyToken, IEnumerable<KeyValuePair<string, TinyToken>>
    {
        private readonly KeyValuePairCollection properties = new KeyValuePairCollection();

        public override bool IsInline => false;
        public override bool IsEmpty => properties.Count == 0;
        public override TinyTokenType Type => TinyTokenType.Object;

        public TinyToken this[string key]
        {
            get => properties[key].Value;
            set => properties.Set(key, value);
        }

        public int Count => properties.Count;

        public void Add(string key, object value) => properties.Add(key, ToToken(value));
        public void Add(string key, TinyToken value) => properties.Add(key, value);
        public void Add(KeyValuePair<string, TinyToken> item) => properties.Add(item.Key, item.Value);

        public IEnumerator<KeyValuePair<string, TinyToken>> GetEnumerator() => properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => properties.GetEnumerator();

        public override T Value<T>(object key)
        {
            if (key == null)
                return (T)(object)this;

            if (key is string k)
            {
                if (properties.TryGetValue(k, out var property))
                    return property.Value.Value<T>();
                else return default(T);
            }
            else if (key is int index)
                return properties[index].Value.Value<T>();

            throw new ArgumentException($"Key must be an integer or a string, was {key}", "key");
        }

        internal override void WriteInternal(TextWriter writer, TinyToken parent, int indentLevel)
        {
            var parentIsArray = parent != null && parent.Type == TinyTokenType.Array;

            var first = true;
            foreach (var property in properties)
            {
                if (!first || !parentIsArray)
                    WriteIndent(writer, indentLevel);

                var key = property.Key;
                if (key.Contains(" ") || key.Contains(":") || key.StartsWith("-"))
                    key = "\"" + TinyUtil.EscapeString(key) + "\"";

                var value = property.Value;
                if (value.IsEmpty)
                    writer.WriteLine(key + ":");
                else if (value.IsInline)
                {
                    writer.Write(key + ": ");
                    value.WriteInternal(writer, this, 0);
                }
                else
                {
                    writer.WriteLine(key + ":");
                    value.WriteInternal(writer, this, indentLevel + 1);
                }
                first = false;
            }
        }

        public override string ToString() => string.Join(", ", properties);

        private class KeyValuePairCollection : Collection<KeyValuePair<string, TinyToken>>
        {
            private readonly Dictionary<string, KeyValuePair<string, TinyToken>> pairs = new Dictionary<string, KeyValuePair<string, TinyToken>>();

            public KeyValuePair<string, TinyToken> this[string key] => pairs[key];

            public void Add(string key, TinyToken value)
                => Add(new KeyValuePair<string, TinyToken>(key, value));

            public void Set(string key, TinyToken value)
                => Set(new KeyValuePair<string, TinyToken>(key, value));

            public void Set(KeyValuePair<string, TinyToken> value)
            {
                if (pairs.TryGetValue(value.Key, out var existingValue))
                    this[Items.IndexOf(value)] = value;
                else Add(value);
            }

            public bool TryGetValue(string key, out KeyValuePair<string, TinyToken> value)
                => pairs.TryGetValue(key, out value);

            #region Collection<T>

            protected override void ClearItems()
            {
                pairs.Clear();
                base.ClearItems();
            }

            protected override void InsertItem(int index, KeyValuePair<string, TinyToken> item)
            {
                pairs[item.Key] = item;
                base.InsertItem(index, item);
            }

            protected override void RemoveItem(int index)
            {
                var indexKey = Items[index].Key;
                pairs.Remove(indexKey);
                base.RemoveItem(index);
            }

            protected override void SetItem(int index, KeyValuePair<string, TinyToken> item)
            {
                var itemKey = item.Key;
                var indexKey = Items[index].Key;

                if (indexKey == itemKey)
                    pairs[itemKey] = item;
                else
                {
                    pairs[itemKey] = item;
                    if (indexKey != null)
                        pairs.Remove(indexKey);
                }
                base.SetItem(index, item);
            }

            #endregion
        }
    }
}
