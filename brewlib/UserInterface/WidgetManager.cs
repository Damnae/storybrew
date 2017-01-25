using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Input;
using BrewLib.ScreenLayers;
using BrewLib.UserInterface.Skinning;
using BrewLib.Util;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BrewLib.UserInterface
{
    public class WidgetManager : InputHandler, IDisposable
    {
        private InputManager inputManager;
        public InputManager InputManager => inputManager;

        private ScreenLayerManager screenLayerManager;
        public ScreenLayerManager ScreenLayerManager => screenLayerManager;

        private Widget root;
        public Vector2 Size
        {
            get { return root.Size; }
            set { root.Size = value; }
        }
        public float Opacity
        {
            get { return root.Opacity; }
            set { root.Opacity = value; }
        }
        private Widget publicRoot;
        public Widget Root => publicRoot;
        private Widget tooltipOverlay;

        private Dictionary<MouseButton, Widget> dragTargets = new Dictionary<MouseButton, Widget>();

        private Widget hoveredWidget;
        public Widget HoveredWidget => hoveredWidget;

        private Widget keyboardFocus;
        public Widget KeyboardFocus
        {
            get { return keyboardFocus; }
            set
            {
                if (keyboardFocus == value) return;

                if (keyboardFocus != null)
                {
                    var e = new WidgetFocusEventArgs(false);
                    fire((w, evt) => w.NotifyFocusChange(evt, e), keyboardFocus, value);
                }

                var previousFocus = keyboardFocus;
                keyboardFocus = value;

                if (keyboardFocus != null)
                {
                    var e = new WidgetFocusEventArgs(true);
                    fire((w, evt) => w.NotifyFocusChange(evt, e), keyboardFocus, previousFocus);
                }
            }
        }

        private Vector2 mousePosition;
        public Vector2 MousePosition => mousePosition;

        private Camera camera;
        public Camera Camera
        {
            get { return camera; }
            set
            {
                if (camera == value) return;
                if (camera != null) camera.Changed -= camera_Changed;
                camera = value;
                if (camera != null) camera.Changed += camera_Changed;
                RefreshHover();
            }
        }

        public readonly Skin Skin;

        public WidgetManager(ScreenLayerManager screenLayerManager, InputManager inputManager, Skin skin)
        {
            this.screenLayerManager = screenLayerManager;
            this.inputManager = inputManager;
            Skin = skin;

            root = new StackLayout(this) { FitChildren = true, };
            root.Add(publicRoot = new StackLayout(this) { FitChildren = true, });
            root.Add(tooltipOverlay = new Widget(this) { Hoverable = false, });
        }

        public void RefreshHover()
        {
            if (camera != null && inputManager.HasMouseFocus)
            {
                mousePosition = camera.FromScreen(inputManager.MousePosition).Xy;
                changeHoveredWidget(root.GetWidgetAt(mousePosition.X, mousePosition.Y));
            }
            else changeHoveredWidget(null);
        }

        public void NotifyWidgetDisposed(Widget widget)
        {
            if (hoveredWidget == widget)
                RefreshHover();
            if (keyboardFocus == widget)
                keyboardFocus = null;

            var keys = new List<MouseButton>(dragTargets.Keys);
            foreach (var key in keys)
                if (dragTargets[key] == widget)
                    dragTargets[key] = null;
        }

        public void Draw(DrawContext drawContext)
        {
            if (root.Visible)
                root.Draw(drawContext, 1);
        }

        #region Tooltip

        private Dictionary<Widget, Widget> tooltips = new Dictionary<Widget, Widget>();

        public void RegisterTooltip(Widget widget, string text)
        {
            RegisterTooltip(widget, new Label(this)
            {
                StyleName = "tooltip",
                AnchorTarget = widget,
                Text = text,
            });
        }

        /// <summary>
        /// Note: The tooltip widget will be disposed when unregistered.
        /// </summary>
        public void RegisterTooltip(Widget widget, Widget tooltip)
        {
            UnregisterTooltip(widget);

            tooltip.Displayed = false;

            tooltips.Add(widget, tooltip);
            tooltipOverlay.Add(tooltip);
            widget.OnHovered += TooltipWidget_OnHovered;

            if (widget == hoveredWidget)
                displayTooltip(tooltip);
        }

        public void UnregisterTooltip(Widget widget)
        {
            Widget tooltip;
            if (tooltips.TryGetValue(widget, out tooltip))
            {
                tooltips.Remove(widget);
                tooltip.Dispose();
                widget.OnHovered -= TooltipWidget_OnHovered;
            }
        }

        private void TooltipWidget_OnHovered(WidgetEvent evt, WidgetHoveredEventArgs e)
        {
            var tooltip = tooltips[evt.Listener];
            if (e.Hovered) displayTooltip(tooltip);
            else tooltip.Displayed = false;
        }

        private void displayTooltip(Widget tooltip)
        {
            var rootBounds = root.Bounds;

            // Attempt to show the tooltip on top of its target

            var targetBounds = tooltip.AnchorTarget.Bounds;
            var topSpace = targetBounds.Top - rootBounds.Top;
            tooltip.Offset = Vector2.Zero;
            tooltip.AnchorFrom = BoxAlignment.Bottom;
            tooltip.AnchorTo = BoxAlignment.Top;
            tooltip.Pack(0, 0, 600, topSpace - 16);

            // Only put it on the bottom if it doesn't fit on top

            var bounds = tooltip.Bounds;
            if (bounds.Top < rootBounds.Top + 16)
            {
                var bottomSpace = rootBounds.Bottom - targetBounds.Bottom;
                if (bottomSpace > topSpace)
                {
                    tooltip.AnchorFrom = BoxAlignment.Top;
                    tooltip.AnchorTo = BoxAlignment.Bottom;
                    tooltip.Pack(0, 0, 600, bottomSpace - 16);
                    bounds = tooltip.Bounds;
                }
            }

            // Adjust its position

            var offsetX = 0f;
            if (bounds.Right > rootBounds.Right - 16)
                offsetX = rootBounds.Right - 16 - bounds.Right;
            else if (bounds.Left < rootBounds.Left + 16)
                offsetX = rootBounds.Left + 16 - bounds.Left;
            tooltip.Offset = new Vector2(offsetX, 0);

            tooltip.Displayed = true;
        }

        #endregion

        #region Placement

        private bool needsAnchorUpdate;
        private bool refreshingAnchors;
        private int anchoringIteration;

        public void InvalidateAnchors()
        {
            needsAnchorUpdate = true;

            if (!keyboardFocus?.Visible ?? false)
                KeyboardFocus = null;
        }

        public void RefreshAnchors()
        {
            if (!needsAnchorUpdate || refreshingAnchors) return;

            try
            {
                refreshingAnchors = true;
                var iterationBefore = anchoringIteration;

                root.PreLayout();
                while (needsAnchorUpdate)
                {
                    needsAnchorUpdate = false;
                    if (anchoringIteration - iterationBefore > 8)
                    {
                        Debug.Print("Could not resolve ui layout");
                        break;
                    }
                    root.UpdateAnchoring(++anchoringIteration);
                }
                RefreshHover();
            }
            finally
            {
                refreshingAnchors = false;
            }
        }

        public float PixelSize => 1 / ((camera as CameraOrtho)?.HeightScaling ?? 1);

        public double SnapToPixel(double value)
        {
            var scaling = (camera as CameraOrtho)?.HeightScaling ?? 1;
            return Math.Round(value * scaling) / scaling;
        }

        public Vector2 SnapToPixel(Vector2 value)
        {
            var scaling = (camera as CameraOrtho)?.HeightScaling ?? 1;
            return new Vector2((float)Math.Round(value.X * scaling) / scaling, (float)Math.Round(value.Y * scaling) / scaling);
        }

        #endregion

        #region Input events

        public void OnFocusChanged(FocusChangedEventArgs e) => RefreshHover();
        public bool OnClickDown(MouseButtonEventArgs e)
        {
            var target = hoveredWidget ?? root;
            if (keyboardFocus != null && target != keyboardFocus && !target.HasAncestor(keyboardFocus))
                KeyboardFocus = null;

            var widgetEvent = fire((w, evt) => w.NotifyClickDown(evt, e), target);
            if (widgetEvent.Handled)
                dragTargets[e.Button] = widgetEvent.Listener;

            return widgetEvent.Handled;
        }
        public bool OnClickUp(MouseButtonEventArgs e)
        {
            Widget dragTarget;
            if (dragTargets.TryGetValue(e.Button, out dragTarget))
                dragTargets[e.Button] = null;

            var target = dragTarget ?? hoveredWidget ?? root;
            return fire((w, evt) => w.NotifyClickUp(evt, e), target).Handled;
        }
        public void OnMouseMove(MouseMoveEventArgs e)
        {
            RefreshHover();
            foreach (var dragTarget in dragTargets.Values)
                if (dragTarget != null)
                    fire((w, evt) => w.NotifyDrag(evt, e), dragTarget);
        }
        public bool OnMouseWheel(MouseWheelEventArgs e) => fire((w, evt) => w.NotifyMouseWheel(evt, e), hoveredWidget ?? root).Handled;
        public bool OnKeyDown(KeyboardKeyEventArgs e) => fire((w, evt) => w.NotifyKeyDown(evt, e), keyboardFocus ?? hoveredWidget ?? root).Handled;
        public bool OnKeyUp(KeyboardKeyEventArgs e) => fire((w, evt) => w.NotifyKeyUp(evt, e), keyboardFocus ?? hoveredWidget ?? root).Handled;
        public bool OnKeyPress(KeyPressEventArgs e) => fire((w, evt) => w.NotifyKeyPress(evt, e), keyboardFocus ?? hoveredWidget ?? root).Handled;

        private void changeHoveredWidget(Widget widget)
        {
            if (widget == hoveredWidget) return;

            if (hoveredWidget != null)
            {
                var e = new WidgetHoveredEventArgs(false);
                fire((w, evt) => w.NotifyHoveredWidgetChange(evt, e), hoveredWidget, widget);
            }

            var previousWidget = hoveredWidget;
            hoveredWidget = widget;

            if (hoveredWidget != null)
            {
                var e = new WidgetHoveredEventArgs(true);
                fire((w, evt) => w.NotifyHoveredWidgetChange(evt, e), hoveredWidget, previousWidget);
            }
        }

        private static WidgetEvent fire(Func<Widget, WidgetEvent, bool> notify, Widget target, Widget relatedTarget = null, bool bubbles = true)
        {
            if (target.IsDisposed) throw new ObjectDisposedException(nameof(target));

            var widgetEvent = new WidgetEvent(target, relatedTarget);
            var ancestors = bubbles ? target.GetAncestors() : null;

            widgetEvent.Listener = target;
            if (notify(target, widgetEvent))
                return widgetEvent;

            if (ancestors != null)
                foreach (var ancestor in ancestors)
                {
                    widgetEvent.Listener = ancestor;
                    if (notify(ancestor, widgetEvent))
                        return widgetEvent;
                }

            return widgetEvent;
        }

        #endregion

        private void camera_Changed(object sender, EventArgs e) => InvalidateAnchors();

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (camera != null) camera.Changed -= camera_Changed;
                root.Dispose();
            }
            camera = null;
            root = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
