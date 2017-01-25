using BrewLib.Graphics;
using BrewLib.Graphics.Drawables;
using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BrewLib.UserInterface
{
    public class Widget : IDisposable
    {
        private static int nextId;
        public readonly int Id = nextId++;

        private WidgetManager manager;
        public WidgetManager Manager => manager;

        private bool displayed = true;
        public bool Displayed
        {
            get { return displayed; }
            set
            {
                if (displayed == value) return;
                displayed = value;
                OnDisplayedChanged?.Invoke(this, EventArgs.Empty);
                if (hoverable) manager.RefreshHover();
            }
        }
        public bool Visible => displayed && (parent == null || parent.Visible);
        public event EventHandler OnDisplayedChanged;

        private bool hoverable = true;
        public bool Hoverable
        {
            get { return hoverable; }
            set
            {
                if (hoverable == value) return;
                hoverable = value;
                if (Visible) manager.RefreshHover();
            }
        }

        private bool clipChildren;
        public bool ClipChildren
        {
            get { return clipChildren; }
            set
            {
                if (clipChildren == value) return;
                clipChildren = value;
                if (Visible && hoverable) manager.RefreshHover();
            }
        }

        public float Opacity = 1;

        private string styleName;
        public string StyleName
        {
            get { return styleName; }
            set
            {
                if (styleName == value) return;
                styleName = value;
                RefreshStyle();
            }
        }

        private Drawable background = NullDrawable.Instance;
        public Drawable Background
        {
            get { return background; }
            set
            {
                if (background == value) return;
                background = value;
                InvalidateLayout();
            }
        }

        private Drawable foreground = NullDrawable.Instance;
        public Drawable Foreground
        {
            get { return foreground; }
            set
            {
                if (foreground == value) return;
                foreground = value;
                InvalidateLayout();
            }
        }

        private string tooltip;
        public string Tooltip
        {
            get { return tooltip; }
            set
            {
                if (tooltip == value) return;
                tooltip = value;

                if (string.IsNullOrWhiteSpace(tooltip))
                {
                    Manager.UnregisterTooltip(this);
                    tooltip = null;
                }
                else Manager.RegisterTooltip(this, tooltip);
            }
        }

        public Widget(WidgetManager manager)
        {
            this.manager = manager;
        }

        public Widget GetWidgetAt(float x, float y)
        {
            if (!displayed || !hoverable)
                return null;

            var position = AbsolutePosition;
            var overThis = x >= position.X && x < position.X + size.X
                        && y >= position.Y && y < position.Y + size.Y;

            if (ClipChildren && !overThis)
                return null;

            for (var i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];
                var result = child.GetWidgetAt(x, y);
                if (result != null) return result;
            }
            return overThis ? this : null;
        }

        public void Draw(DrawContext drawContext, float parentOpacity)
        {
            var actualOpacity = Opacity * parentOpacity;
            DrawBackground(drawContext, actualOpacity);
            DrawChildren(drawContext, actualOpacity);
            DrawForeground(drawContext, actualOpacity);
        }

        protected virtual void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            background?.Draw(drawContext, manager.Camera, Bounds, actualOpacity);
        }

        protected virtual void DrawChildren(DrawContext drawContext, float actualOpacity)
        {
            if (children.Count == 0)
                return;

#if DEBUG
            Box2? clipRegionDebug = null;
#endif
            using (ClipChildren ? DrawState.Clip(Bounds, Manager.Camera) : null)
            {
                foreach (var child in children)
                    if (child.displayed)
                        child.Draw(drawContext, actualOpacity);

#if DEBUG
                if (ClipChildren)
                    clipRegionDebug = DrawState.GetClipRegion(Manager.Camera);
#endif
            }
#if DEBUG
            if (clipRegionDebug.HasValue)
                Manager.Skin.GetDrawable("debug_clipregion")?.Draw(drawContext, manager.Camera, clipRegionDebug.Value, 1);
#endif
        }

        protected virtual void DrawForeground(DrawContext drawContext, float actualOpacity)
        {
            foreground?.Draw(drawContext, manager.Camera, Bounds, actualOpacity);

#if DEBUG
            manager.Skin.GetDrawable("debug")?.Draw(drawContext, manager.Camera, Bounds, 1);

            var relayout = Math.Max(0, (lastLayoutTime + 1) - manager.ScreenLayerManager.TimeSource.Current);
            if (relayout > 0) manager.Skin.GetDrawable("debug_relayout")?.Draw(drawContext, manager.Camera, Bounds, (float)relayout);
#endif
        }

        #region Styling

        protected virtual WidgetStyle Style => Manager.Skin.GetStyle<WidgetStyle>(StyleName);

        public void RefreshStyle()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Widget));

            var style = Style;
            if (style != null) ApplyStyle(style);
        }

        protected virtual void ApplyStyle(WidgetStyle style)
        {
            Background = style.Background;
            Foreground = style.Foreground;
        }

        public string BuildStyleName(params string[] modifiers)
            => buildStyleName(StyleName, modifiers);

        private static string buildStyleName(string baseName, params string[] modifiers)
        {
            if (modifiers.Length == 0)
                return baseName;

            var sb = new StringBuilder();
            sb.Append(baseName);
            foreach (var modifier in modifiers)
            {
                if (string.IsNullOrEmpty(modifier))
                    continue;

                sb.Append(" #");
                sb.Append(modifier);
            }
            return sb.ToString();
        }

        #endregion

        #region Parenting

        private Widget parent;
        public Widget Parent => parent;
        private List<Widget> children = new List<Widget>();
        public IEnumerable<Widget> Children
        {
            get { return children; }
            set
            {
                ClearWidgets();
                foreach (var widget in value)
                    Add(widget);
            }
        }

        public void Add(Widget widget)
        {
            if (children.Contains(widget)) return;

            if (widget == this) throw new InvalidOperationException("Cannot parent a widget to itself");
            if (widget.HasDescendant(this)) throw new InvalidOperationException("Cannot recursively parent a widget to itself");

            widget.parent?.Remove(widget);
            children.Add(widget);
            widget.parent = this;

            if (widget.StyleName == null)
                widget.StyleName = "default";

            InvalidateAncestorLayout();
        }

        public void Remove(Widget widget)
        {
            if (!children.Contains(widget)) return;

            widget.parent = null;
            children.Remove(widget);

            InvalidateAncestorLayout();
        }

        public void ClearWidgets()
        {
            var childrenSnapshot = new List<Widget>(children);
            foreach (var child in childrenSnapshot)
                child.Dispose();
        }

        public bool HasAncestor(Widget widget)
        {
            if (parent == null) return false;
            if (parent == widget) return true;
            return parent.HasAncestor(widget);
        }

        public bool HasDescendant(Widget widget)
        {
            foreach (Widget child in children)
                if (child == widget || child.HasDescendant(widget))
                    return true;
            return false;
        }

        public List<Widget> GetAncestors()
        {
            var ancestors = new List<Widget>();
            var ancestor = parent;
            while (ancestor != null)
            {
                ancestors.Add(ancestor);
                ancestor = ancestor.Parent;
            }
            return ancestors;
        }

        #endregion

        #region Placement

        private Vector2 offset;
        public Vector2 Offset
        {
            get { return offset; }
            set
            {
                if (offset == value) return;
                offset = value;
                manager.InvalidateAnchors();
            }
        }

        private Vector2 size;
        public Vector2 Size
        {
            get { return size; }
            set
            {
                if (size == value) return;
                size = value;
                InvalidateLayout();
            }
        }
        public float Width
        {
            get { return Size.X; }
            set { Size = new Vector2(value, Size.Y); }
        }
        public float Height
        {
            get { return Size.Y; }
            set { Size = new Vector2(Size.X, value); }
        }

        private Vector2 absolutePosition;
        public Vector2 AbsolutePosition
        {
            get
            {
                manager.RefreshAnchors();
                return absolutePosition;
            }
        }

        public Box2 Bounds =>
            new Box2(AbsolutePosition, AbsolutePosition + Size);

        private Widget anchorTarget;
        public Widget AnchorTarget
        {
            get { return anchorTarget; }
            set
            {
                if (anchorTarget == value) return;
                anchorTarget = value;
                manager.InvalidateAnchors();
            }
        }

        private BoxAlignment anchorFrom = BoxAlignment.TopLeft;
        public BoxAlignment AnchorFrom
        {
            get { return anchorFrom; }
            set
            {
                if (anchorFrom == value) return;
                anchorFrom = value;
                manager.InvalidateAnchors();
            }
        }

        private BoxAlignment anchorTo = BoxAlignment.TopLeft;
        public BoxAlignment AnchorTo
        {
            get { return anchorTo; }
            set
            {
                if (anchorTo == value) return;
                anchorTo = value;
                manager.InvalidateAnchors();
            }
        }

        private int anchoringIteration;

        public void UpdateAnchoring(int iteration, bool includeChildren = true)
        {
            ValidateLayout();
            if (anchoringIteration < iteration)
            {
                anchoringIteration = iteration;

                var actualAnchorTarget = anchorTarget != null && (anchorTarget.parent != null || anchorTarget == manager.Root) ? anchorTarget : parent;
                if (actualAnchorTarget != null)
                {
                    actualAnchorTarget.UpdateAnchoring(iteration, false);
                    absolutePosition = actualAnchorTarget.absolutePosition + offset;

                    if (anchorFrom.HasFlag(BoxAlignment.Right))
                        absolutePosition.X -= size.X;
                    else if (!anchorFrom.HasFlag(BoxAlignment.Left))
                        absolutePosition.X -= size.X * 0.5f;

                    if (anchorFrom.HasFlag(BoxAlignment.Bottom))
                        absolutePosition.Y -= size.Y;
                    else if (!anchorFrom.HasFlag(BoxAlignment.Top))
                        absolutePosition.Y -= size.Y * 0.5f;

                    if (anchorTo.HasFlag(BoxAlignment.Right))
                        absolutePosition.X += actualAnchorTarget.Size.X;
                    else if (!anchorTo.HasFlag(BoxAlignment.Left))
                        absolutePosition.X += actualAnchorTarget.Size.X * 0.5f;

                    if (anchorTo.HasFlag(BoxAlignment.Bottom))
                        absolutePosition.Y += actualAnchorTarget.Size.Y;
                    else if (!anchorTo.HasFlag(BoxAlignment.Top))
                        absolutePosition.Y += actualAnchorTarget.Size.Y * 0.5f;
                }
                else absolutePosition = offset;
                absolutePosition = manager.SnapToPixel(absolutePosition);
            }

            if (includeChildren)
                foreach (var child in children)
                    child.UpdateAnchoring(iteration);
        }

        #endregion

        #region Layout

        public virtual Vector2 MinSize => PreferredSize;
        public virtual Vector2 MaxSize => Vector2.Zero;
        public virtual Vector2 PreferredSize => Vector2.Zero;

        private bool canGrow = true;
        public bool CanGrow
        {
            get { return canGrow; }
            set
            {
                if (canGrow == value) return;
                canGrow = value;
                InvalidateAncestorLayout();
            }
        }

        private bool needsLayout = true;
        public bool NeedsLayout => needsLayout;

        private double lastLayoutTime = double.MinValue;

        public void Pack(float width = 0, float height = 0, float maxWidth = 0, float maxHeight = 0)
        {
            var preferredSize = PreferredSize;

            var newSize = preferredSize;
            if (width > 0 && (maxWidth == 0 || (maxWidth > 0 && newSize.X < width))) newSize.X = width;
            if (height > 0 && (maxHeight == 0 || (maxHeight > 0 && newSize.Y < height))) newSize.Y = height;
            if (maxWidth > 0 && newSize.X > maxWidth) newSize.X = maxWidth;
            if (maxHeight > 0 && newSize.Y > maxHeight) newSize.Y = maxHeight;
            Size = newSize;

            // Flow layouts and labels don't know their height until they know their width
            manager.RefreshAnchors();
            if (preferredSize != PreferredSize)
                Pack(width, height, maxWidth, maxHeight);
        }

        public void InvalidateAncestorLayout()
        {
            InvalidateLayout();
            parent?.InvalidateAncestorLayout();
        }

        public virtual void InvalidateLayout()
        {
            needsLayout = true;
            manager.InvalidateAnchors();
        }

        public void ValidateLayout()
        {
            if (!needsLayout) return;
            Layout();
        }

        public virtual void PreLayout()
        {
            foreach (var child in children)
                child.PreLayout();
        }

        protected virtual void Layout()
        {
            lastLayoutTime = manager.ScreenLayerManager.TimeSource.Current;
            needsLayout = false;
        }

        #endregion

        #region Events

        public delegate void WidgetEventHandler<TEventArgs>(WidgetEvent evt, TEventArgs e);
        public delegate bool HandleableWidgetEventHandler<TEventArgs>(WidgetEvent evt, TEventArgs e);

        public event HandleableWidgetEventHandler<MouseButtonEventArgs> OnClickDown;
        public bool NotifyClickDown(WidgetEvent evt, MouseButtonEventArgs e) => Raise(OnClickDown, evt, e);

        public event WidgetEventHandler<MouseButtonEventArgs> OnClickUp;
        public bool NotifyClickUp(WidgetEvent evt, MouseButtonEventArgs e)
        {
            Raise(OnClickUp, evt, e);
            return false;
        }

        public event WidgetEventHandler<MouseMoveEventArgs> OnDrag;
        public bool NotifyDrag(WidgetEvent evt, MouseMoveEventArgs e)
        {
            Raise(OnDrag, evt, e);
            return false;
        }

        public event HandleableWidgetEventHandler<MouseWheelEventArgs> OnMouseWheel;
        public bool NotifyMouseWheel(WidgetEvent evt, MouseWheelEventArgs e) => Raise(OnMouseWheel, evt, e);

        public event HandleableWidgetEventHandler<KeyboardKeyEventArgs> OnKeyDown;
        public bool NotifyKeyDown(WidgetEvent evt, KeyboardKeyEventArgs e) => Raise(OnKeyDown, evt, e);

        public event HandleableWidgetEventHandler<KeyboardKeyEventArgs> OnKeyUp;
        public bool NotifyKeyUp(WidgetEvent evt, KeyboardKeyEventArgs e) => Raise(OnKeyUp, evt, e);

        public event HandleableWidgetEventHandler<KeyPressEventArgs> OnKeyPress;
        public bool NotifyKeyPress(WidgetEvent evt, KeyPressEventArgs e) => Raise(OnKeyPress, evt, e);

        public event WidgetEventHandler<WidgetHoveredEventArgs> OnHovered;
        public event WidgetEventHandler<WidgetHoveredEventArgs> OnHoveredWidgetChange;
        public bool NotifyHoveredWidgetChange(WidgetEvent evt, WidgetHoveredEventArgs e)
        {
            var related = evt.RelatedTarget;
            while (related != null && related != this)
                related = related.parent;

            if (related != this)
                Raise(OnHovered, evt, e);

            Raise(OnHoveredWidgetChange, evt, e);
            return false;
        }

        public event WidgetEventHandler<WidgetFocusEventArgs> OnFocusChange;
        public bool NotifyFocusChange(WidgetEvent evt, WidgetFocusEventArgs e)
        {
            Raise(OnFocusChange, evt, e);
            return false;
        }

        protected static bool Raise<T>(HandleableWidgetEventHandler<T> handler, WidgetEvent evt, T e)
        {
            if (handler != null)
                foreach (var handlerDelegate in handler.GetInvocationList())
                    try
                    {
                        if (!Array.Exists(handler.GetInvocationList(), h => h == handlerDelegate))
                            continue;

                        if (((HandleableWidgetEventHandler<T>)handlerDelegate)(evt, e))
                        {
                            evt.Handled = true;
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Event handler '{handler.Method}' for '{handler.Target}' raised an exception:\n{exception}");
                    }

            return evt.Handled;
        }

        protected static void Raise<T>(WidgetEventHandler<T> handler, WidgetEvent evt, T e)
            => EventHelper.InvokeStrict(() => handler, d => ((WidgetEventHandler<T>)d)(evt, e));

        public event EventHandler OnDisposed;

        #endregion

        #region IDisposable Support

        public bool IsDisposed => disposedValue;

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Tooltip = null;

                    parent?.Remove(this);
                    manager.NotifyWidgetDisposed(this);
                    ClearWidgets();
                }
                children = null;

                disposedValue = true;
                if (disposing)
                    OnDisposed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public override string ToString() => $"{GetType().Name} {StyleName} #{Id} {Width}x{Height}";
    }
}
