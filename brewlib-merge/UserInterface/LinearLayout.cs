using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using OpenTK;
using System;
using System.Collections.Generic;

namespace BrewLib.UserInterface
{
    public class LinearLayout : Widget
    {
        Vector2 minSize;
        Vector2 preferredSize;
        bool invalidSizes = true;

        public override Vector2 MinSize
        {
            get
            {
                measureChildren();
                return minSize;
            }
        }
        public override Vector2 PreferredSize
        {
            get
            {
                measureChildren();
                return preferredSize;
            }
        }

        bool horizontal;
        public bool Horizontal
        {
            get => horizontal;
            set
            {
                if (horizontal == value) return;
                horizontal = value;
                InvalidateAncestorLayout();
            }
        }

        float spacing;
        public float Spacing
        {
            get => spacing;
            set
            {
                if (spacing == value) return;
                spacing = value;
                InvalidateAncestorLayout();
            }
        }

        FourSide padding;
        public FourSide Padding
        {
            get => padding;
            set
            {
                if (padding == value) return;
                padding = value;
                InvalidateAncestorLayout();
            }
        }

        bool fitChildren;
        public bool FitChildren
        {
            get => fitChildren;
            set
            {
                if (fitChildren == value) return;
                fitChildren = value;
                InvalidateLayout();
            }
        }

        bool fill;
        public bool Fill
        {
            get => fill;
            set
            {
                if (fill == value) return;
                fill = value;
                InvalidateLayout();
            }
        }
        public LinearLayout(WidgetManager manager) : base(manager) { }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<LinearLayoutStyle>(StyleName);
        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var layoutStyle = (LinearLayoutStyle)style;

            Spacing = layoutStyle.Spacing;
        }
        public override void InvalidateLayout()
        {
            base.InvalidateLayout();
            invalidSizes = true;
        }
        protected override void Layout()
        {
            base.Layout();

            // Prepare preferred lengths

            var innerSize = new Vector2(Size.X - padding.Horizontal, Size.Y - padding.Vertical);
            var totalSpace = horizontal ? innerSize.X : innerSize.Y;
            var usedSpace = 0f;

            var items = new List<LayoutItem>();
            foreach (var child in Children)
            {
                if (child.AnchorTarget != null) continue;

                var preferredSize = child.PreferredSize;
                var minSize = child.MinSize;
                var maxSize = child.MaxSize;
                var length = horizontal ? preferredSize.X : preferredSize.Y;

                items.Add(new LayoutItem
                {
                    Widget = child,
                    PreferredSize = preferredSize,
                    MinSize = minSize,
                    MaxSize = maxSize,
                    Length = length,
                    Scalable = true
                });
                usedSpace += length;
            }
            var totalSpacing = spacing * (items.Count - 1);
            usedSpace += totalSpacing;

            var scalableItems = items.Count;
            while (scalableItems > 0 && Math.Abs(totalSpace - usedSpace) > 0.001f)
            {
                var remainingSpace = totalSpace - usedSpace;
                if (!fill && remainingSpace > 0) break;

                var adjustment = remainingSpace / scalableItems;
                usedSpace = totalSpacing;
                scalableItems = 0;

                foreach (var item in items)
                {
                    if (!item.Widget.CanGrow && adjustment > 0) item.Scalable = false;

                    if (item.Scalable)
                    {
                        item.Length += adjustment;
                        if (horizontal)
                        {
                            if (item.Length < item.MinSize.X)
                            {
                                item.Length = item.MinSize.X;
                                item.Scalable = false;
                            }
                            else if (item.MaxSize.X > 0 && item.Length >= item.MaxSize.X)
                            {
                                item.Length = item.MaxSize.X;
                                item.Scalable = false;
                            }
                            else scalableItems++;
                        }
                        else
                        {
                            if (item.Length < item.MinSize.Y)
                            {
                                item.Length = item.MinSize.Y;
                                item.Scalable = false;
                            }
                            else if (item.MaxSize.Y > 0 && item.Length >= item.MaxSize.Y)
                            {
                                item.Length = item.MaxSize.Y;
                                item.Scalable = false;
                            }
                            else scalableItems++;
                        }
                    }
                    usedSpace += item.Length;
                }
            }

            var distance = horizontal ? padding.Left : padding.Top;
            foreach (var item in items)
            {
                var child = item.Widget;
                var minSize = item.MinSize;
                var maxSize = item.MaxSize;

                if (horizontal)
                {
                    var childBreadth = fitChildren ? Math.Max(minSize.Y, innerSize.Y) : Math.Max(minSize.Y, Math.Min(item.PreferredSize.Y, innerSize.Y));
                    if (maxSize.Y > 0 && childBreadth > maxSize.Y) childBreadth = maxSize.Y;

                    var anchor = (child.AnchorFrom & BoxAlignment.Vertical) | BoxAlignment.Left;
                    PlaceChildren(child, new Vector2(distance, padding.GetVerticalOffset(anchor)), new Vector2(item.Length, childBreadth), anchor);
                }
                else
                {
                    var childBreadth = fitChildren ? Math.Max(minSize.X, innerSize.X) : Math.Max(minSize.X, Math.Min(item.PreferredSize.X, innerSize.X));
                    if (maxSize.X > 0 && childBreadth > maxSize.X) childBreadth = maxSize.X;

                    var anchor = (child.AnchorFrom & BoxAlignment.Horizontal) | BoxAlignment.Top;
                    PlaceChildren(child, new Vector2(padding.GetHorizontalOffset(anchor), distance), new Vector2(childBreadth, item.Length), anchor);
                }
                distance += item.Length + spacing;
            }
        }
        protected virtual void PlaceChildren(Widget widget, Vector2 offset, Vector2 size, BoxAlignment anchor)
        {
            widget.Offset = offset;
            widget.Size = size;
            widget.AnchorFrom = anchor;
            widget.AnchorTo = anchor;
        }
        void measureChildren()
        {
            if (!invalidSizes) return;
            invalidSizes = false;

            float width = 0, height = 0;
            float minWidth = 0, minHeight = 0;

            var firstChild = true;
            foreach (var child in Children)
            {
                if (child.AnchorTarget != null) continue;

                var childMinSize = child.MinSize;
                var childSize = child.PreferredSize;
                if (horizontal)
                {
                    height = Math.Max(height, childSize.Y);
                    width += childSize.X;

                    minHeight = Math.Max(minHeight, childMinSize.Y);
                    minWidth += childMinSize.X;

                    if (!firstChild)
                    {
                        width += spacing;
                        minWidth += spacing;
                    }
                }
                else
                {
                    width = Math.Max(width, childSize.X);
                    height += childSize.Y;

                    minWidth = Math.Max(minWidth, childMinSize.X);
                    minHeight += childMinSize.Y;

                    if (!firstChild)
                    {
                        height += spacing;
                        minHeight += spacing;
                    }
                }
                firstChild = false;
            }
            var paddingH = padding.Horizontal;
            var paddingV = padding.Vertical;

            minSize = new Vector2(minWidth + paddingH, minHeight + paddingV);
            preferredSize = new Vector2(width + paddingH, height + paddingV);
        }
        class LayoutItem
        {
            public Widget Widget;
            public Vector2 PreferredSize, MinSize, MaxSize;
            public float Length;
            public bool Scalable;

            public override string ToString() => $"{Widget} Scalable:{Scalable} Length:{Length} PreferredSize:{PreferredSize}";
        }
    }
}