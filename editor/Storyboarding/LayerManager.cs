using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewEditor.Storyboarding
{
    public class LayerManager
    {
        private List<EditorStoryboardLayer> layers = new List<EditorStoryboardLayer>();

        public int LayersCount => layers.Count;
        public IEnumerable<EditorStoryboardLayer> Layers => layers;
        public List<EditorStoryboardLayer> FindLayers(Predicate<EditorStoryboardLayer> predicate) => layers.FindAll(predicate);

        public event EventHandler OnLayersChanged;

        public void Add(EditorStoryboardLayer layer)
        {
            layers.Insert(findLayerIndex(layer), layer);
            layer.OnChanged += layer_OnChanged;
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(EditorStoryboardLayer oldLayer, EditorStoryboardLayer newLayer)
        {
            var index = layers.IndexOf(oldLayer);
            if (index != -1)
            {
                newLayer.CopySettings(oldLayer);
                newLayer.OnChanged += layer_OnChanged;
                oldLayer.OnChanged -= layer_OnChanged;
                layers[index] = newLayer;
            }
            else throw new InvalidOperationException($"Cannot replace layer '{oldLayer.Name}' with '{newLayer.Name}', old layer not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(List<EditorStoryboardLayer> oldLayers, List<EditorStoryboardLayer> newLayers)
        {
            oldLayers = new List<EditorStoryboardLayer>(oldLayers);
            foreach (var newLayer in newLayers)
            {
                var oldLayer = oldLayers.Find(l => l.Identifier == newLayer.Identifier);
                if (oldLayer != null)
                {
                    var index = layers.IndexOf(oldLayer);
                    if (index != -1)
                    {
                        newLayer.CopySettings(layers[index]);
                        layers[index] = newLayer;
                    }
                    oldLayers.Remove(oldLayer);
                }
                else layers.Insert(findLayerIndex(newLayer), newLayer);
                newLayer.OnChanged += layer_OnChanged;
            }
            foreach (var oldLayer in oldLayers)
            {
                oldLayer.OnChanged -= layer_OnChanged;
                layers.Remove(oldLayer);
            }
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(EditorStoryboardLayer oldLayer, List<EditorStoryboardLayer> newLayers)
        {
            var index = layers.IndexOf(oldLayer);
            if (index != -1)
            {
                foreach (var newLayer in newLayers)
                {
                    newLayer.CopySettings(oldLayer);
                    newLayer.OnChanged += layer_OnChanged;
                }
                layers.InsertRange(index, newLayers);

                oldLayer.OnChanged -= layer_OnChanged;
                layers.Remove(oldLayer);
            }
            else throw new InvalidOperationException($"Cannot replace layer '{oldLayer.Name}' with multiple layers, old layer not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
            {
                layer.OnChanged -= layer_OnChanged;
                OnLayersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool MoveUp(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                if (index > 0 && layer.CompareTo(layers[index - 1]) == 0)
                {
                    var otherLayer = layers[index - 1];
                    layers[index - 1] = layer;
                    layers[index] = otherLayer;
                }
                else return false;
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}'");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool MoveDown(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                if (index < layers.Count - 1 && layer.CompareTo(layers[index + 1]) == 0)
                {
                    var otherLayer = layers[index + 1];
                    layers[index + 1] = layer;
                    layers[index] = otherLayer;
                }
                else return false;
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}'");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool MoveToTop(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                if (index == 0) return false;
                while (index > 0 && layer.CompareTo(layers[index - 1]) == 0)
                {
                    var otherLayer = layers[index - 1];
                    layers[index - 1] = layer;
                    layers[index] = otherLayer;
                    --index;
                }
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}'");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool MoveToBottom(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                if (index == layers.Count - 1) return false;
                while (index < layers.Count - 1 && layer.CompareTo(layers[index + 1]) == 0)
                {
                    var otherLayer = layers[index + 1];
                    layers[index + 1] = layer;
                    layers[index] = otherLayer;
                    ++index;
                }
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}'");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void TriggerEvents(double startTime, double endTime)
        {
            foreach (var layer in Layers)
                layer.TriggerEvents(startTime, endTime);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            foreach (var layer in Layers)
                layer.Draw(drawContext, camera, bounds, opacity);
        }

        private void layer_OnChanged(object sender, ChangedEventArgs e)
        {
            if (e.PropertyName == null || e.PropertyName == nameof(EditorStoryboardLayer.OsbLayer) || e.PropertyName == nameof(EditorStoryboardLayer.DiffSpecific))
                sortLayer((EditorStoryboardLayer)sender);
        }

        private void sortLayer(EditorStoryboardLayer layer)
        {
            var initialIndex = layers.IndexOf(layer);
            if (initialIndex < 0) new InvalidOperationException($"Layer '{layer.Name}' cannot be found");

            var newIndex = initialIndex;
            while (newIndex > 0 && layer.CompareTo(layers[newIndex - 1]) < 0) newIndex--;
            while (newIndex < layers.Count - 1 && layer.CompareTo(layers[newIndex + 1]) > 0) newIndex++;

            layers.Move(initialIndex, newIndex);
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        private int findLayerIndex(EditorStoryboardLayer layer)
        {
            var index = layers.BinarySearch(layer);
            if (index >= 0)
            {
                while (index < layers.Count && layer.CompareTo(layers[index]) == 0)
                    index++;
                return index;
            }
            else return ~index;
        }
    }
}
