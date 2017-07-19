using BrewLib.Graphics;
using BrewLib.Input;
using BrewLib.Time;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;

namespace BrewLib.ScreenLayers
{
    public class ScreenLayerManager : IDisposable
    {
        private GameWindow window;

        private TimeSource timeSource;
        public TimeSource TimeSource => timeSource;

        private object context;
        public T GetContext<T>() => (T)context;

        private List<ScreenLayer> layers = new List<ScreenLayer>();
        private List<ScreenLayer> updateQueue = new List<ScreenLayer>();
        private List<ScreenLayer> removedLayers = new List<ScreenLayer>();
        private ScreenLayer focusedLayer;

        private InputDispatcher inputDispatcher = new InputDispatcher();
        public InputHandler InputHandler => inputDispatcher;

        public Color4 BackgroundColor => Color4.Black;

        public ScreenLayerManager(GameWindow window, TimeSource timeSource, object context)
        {
            this.window = window;
            this.timeSource = timeSource;
            this.context = context;

            window.Resize += window_Resize;
        }

        public void Add(ScreenLayer layer)
        {
            layer.Manager = this;
            layers.Add(layer);
            layer.Load();

            var width = Math.Max(1, window.Width);
            var height = Math.Max(1, window.Height);
            layer.Resize(width, height);
        }

        public void Set(ScreenLayer layer)
        {
            var layersToExit = new List<ScreenLayer>(layers);
            for (int i = layersToExit.Count - 1; i >= 0; --i)
                layersToExit[i].Exit();
            Add(layer);
        }

        public void Remove(ScreenLayer layer)
        {
            if (focusedLayer == layer)
                changeFocus(null);

            layers.Remove(layer);
            removedLayers.Add(layer);
            updateQueue.Remove(layer);
        }

        public bool Close()
        {
            for (int i = layers.Count - 1; i >= 0; --i)
            {
                var layer = layers[i];
                if (layer.IsExiting)
                    continue;

                layer.Close();
                return true;
            }
            return false;
        }

        public void Exit()
        {
            var snapshot = new List<ScreenLayer>(layers);
            for (int i = snapshot.Count - 1; i >= 0; --i)
            {
                var layer = snapshot[i];
                if (layer.IsExiting)
                    continue;

                layer.Exit();
            }
        }

        public void Update()
        {
            var active = window.Focused;
            if (!active) changeFocus(null);

            updateQueue.Clear();
            updateQueue.AddRange(layers);

            bool covered = false, top = true, hasFocus = active;
            while (updateQueue.Count > 0)
            {
                var layerIndex = updateQueue.Count - 1;
                var layer = updateQueue[layerIndex];
                updateQueue.RemoveAt(layerIndex);

                if (hasFocus)
                {
                    if (layer.IsExiting)
                    {
                        if (focusedLayer == layer)
                            changeFocus(null);
                    }
                    else
                    {
                        if (focusedLayer != layer)
                            changeFocus(layer);
                        hasFocus = false;
                    }
                }

                layer.Update(top, covered);

                if (!layer.IsPopup)
                    covered = true;
                top = false;
            }

            foreach (var layer in removedLayers)
                layer.Dispose();
            removedLayers.Clear();

            if (layers.Count == 0)
                window.Exit();
        }

        public void Draw(DrawContext drawContext, double tween)
        {
            foreach (var layer in layers)
            {
                if (layer.CurrentState == ScreenLayer.State.Hidden)
                    continue;

                layer.Draw(drawContext, tween);
            }
        }

        private void changeFocus(ScreenLayer layer)
        {
            if (focusedLayer != null)
            {
                inputDispatcher.Remove(focusedLayer.InputHandler);
                focusedLayer.LoseFocus();
                focusedLayer = null;
            }

            if (layer != null)
            {
                inputDispatcher.Add(layer.InputHandler);
                layer.GainFocus();
                focusedLayer = layer;
            }
        }

        private void window_Resize(object sender, EventArgs e)
        {
            var width = window.Width;
            var height = window.Height;

            if (width == 0 || height == 0) return;

            foreach (var layer in layers)
                layer.Resize(width, height);
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    changeFocus(null);
                    for (var i = layers.Count - 1; i >= 0; --i)
                    {
                        var layer = layers[i];
                        layer.Dispose();
                    }
                    window.Resize -= window_Resize;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
