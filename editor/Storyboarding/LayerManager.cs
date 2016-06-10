using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorybrewEditor.Storyboarding
{
    public class LayerManager
    {
        private List<EditorStoryboardLayer> layers = new List<EditorStoryboardLayer>();

        public int LayersCount => layers.Count;
        public IEnumerable<EditorStoryboardLayer> Layers => layers.OrderBy(x => x);
        public List<EditorStoryboardLayer> FindLayers(Predicate<EditorStoryboardLayer> predicate) => layers.FindAll(predicate);

        public event EventHandler OnLayersChanged;

        public void Add(EditorStoryboardLayer layer)
        {
            layers.Add(layer);
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(EditorStoryboardLayer oldLayer, EditorStoryboardLayer newLayer)
        {
            var index = layers.IndexOf(oldLayer);
            if (index != -1)
            {
                newLayer.CopySettings(layers[index]);
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
                else layers.Add(newLayer);
            }
            foreach (var oldLayer in oldLayers)
                layers.Remove(oldLayer);
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(EditorStoryboardLayer oldLayer, List<EditorStoryboardLayer> newLayers)
        {
            var index = layers.IndexOf(oldLayer);
            if (index != -1)
            {
                foreach (var newLayer in newLayers)
                    newLayer.CopySettings(oldLayer);
                layers.InsertRange(index, newLayers);
                layers.Remove(oldLayer);
            }
            else throw new InvalidOperationException($"Cannot replace layer '{oldLayer.Name}' with multiple layers, old layer not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
                OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveUp(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                var otherLayer = layers[index - 1];
                layers[index - 1] = layer;
                layers[index] = otherLayer;
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveDown(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                var otherLayer = layers[index + 1];
                layers[index + 1] = layer;
                layers[index] = otherLayer;
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveTop(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
                layers.Insert(0, layer);
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveBottom(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
                layers.Add(layer);
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            foreach (var layer in Layers)
                layer.Draw(drawContext, camera, bounds, opacity);
        }
    }
}
