using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorybrewEditor.Storyboarding
{
    public abstract class Effect : IDisposable
    {
        private List<EditorStoryboardLayer> layers;
        private EditorStoryboardLayer placeHolderLayer;

        public Project Project { get; }

        public Guid Guid { get; set; } = Guid.NewGuid();

        private string name = "Unnamed Effect";
        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;

                name = value;
                RaiseChanged();
                refreshLayerNames();
            }
        }

        public abstract string BaseName { get; }
        public virtual string Path => null;

        public virtual EffectStatus Status { get; }
        public virtual string StatusMessage { get; }

        public virtual bool Multithreaded { get; }
        public virtual bool BeatmapDependant { get; }

        public double StartTime => layers.Select(l => l.StartTime).DefaultIfEmpty().Min();
        public double EndTime => layers.Select(l => l.EndTime).DefaultIfEmpty().Max();
        public bool Highlight;

        public int EstimatedSize { get; private set; }

        public event EventHandler OnChanged;
        protected void RaiseChanged()
            => OnChanged?.Invoke(this, EventArgs.Empty);

        public EffectConfig Config = new EffectConfig();
        public event EventHandler OnConfigFieldsChanged;
        protected void RaiseConfigFieldsChanged()
            => OnConfigFieldsChanged?.Invoke(this, EventArgs.Empty);

        public Effect(Project project)
        {
            Project = project;

            layers = new List<EditorStoryboardLayer>
            {
                (placeHolderLayer = new EditorStoryboardLayer(string.Empty, this))
            };
            refreshLayerNames();
            Project.LayerManager.Add(placeHolderLayer);
        }

        /// <summary>
        /// Used at load time to let the effect know about placeholder layers it should use.
        /// </summary>
        public void AddPlaceholder(EditorStoryboardLayer layer)
        {
            if (placeHolderLayer != null)
            {
                layers.Remove(placeHolderLayer);
                Project.LayerManager.Remove(placeHolderLayer);
                placeHolderLayer = null;
            }
            layers.Add(layer);
            refreshLayerNames();

            Project.LayerManager.Add(layer);
        }

        protected void UpdateLayers(List<EditorStoryboardLayer> newLayers)
        {
            if (placeHolderLayer != null)
            {
                Project.LayerManager.Replace(placeHolderLayer, newLayers);
                placeHolderLayer = null;
            }
            else Project.LayerManager.Replace(layers, newLayers);
            layers = newLayers;
            refreshLayerNames();
            refreshEstimatedSize();
        }

        /// <summary>
        /// Queues an Update call
        /// </summary>
        public void Refresh()
        {
            if (Project.IsDisposed) return;
            Project.QueueEffectUpdate(this);
        }

        /// <summary>
        /// Should only be called by Project.QueueEffectUpdate(Effect).
        /// Doesn't run on the main thread.
        /// </summary>
        public abstract void Update();

        private void refreshLayerNames()
        {
            foreach (var layer in layers)
                layer.Name = string.IsNullOrWhiteSpace(layer.Identifier) ? $"{name}" : $"{name} ({layer.Identifier})";
        }

        private void refreshEstimatedSize()
        {
            EstimatedSize = 0;
            foreach (var layer in layers)
                EstimatedSize += layer.EstimatedSize;
            RaiseChanged();
        }

        #region IDisposable Support

        public bool IsDisposed { get; private set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    foreach (var layer in layers)
                        Project.LayerManager.Remove(layer);
                }
                layers = null;
                OnChanged = null;
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    public enum EffectStatus
    {
        Initializing,
        Loading,
        Configuring,
        Updating,
        ReloadPending,
        Ready,
        CompilationFailed,
        LoadingFailed,
        ExecutionFailed,
    }
}
