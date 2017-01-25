using BrewLib.Graphics;
using BrewLib.UserInterface.Skinning.Styles;
using BrewLib.Util;
using OpenTK;
using System;
using System.Collections.Generic;

namespace BrewLib.UserInterface
{
    public class FlowLayout : Widget
    {
        private Vector2 preferredSize;
        private float flowWidth = 0;
        private List<LayoutLine> lines;
        private bool invalidSizes = true;

        public override Vector2 MinSize { get { measureChildren(); return new Vector2(0, PreferredSize.Y); } }
        public override Vector2 PreferredSize { get { measureChildren(); return preferredSize; } }
        public override Vector2 MaxSize { get { measureChildren(); return preferredSize; } }

        private float spacing;
        public float Spacing
        {
            get { return spacing; }
            set
            {
                if (spacing == value) return;
                spacing = value;
                InvalidateAncestorLayout();
            }
        }

        private float lineSpacing;
        public float LineSpacing
        {
            get { return lineSpacing; }
            set
            {
                if (lineSpacing == value) return;
                lineSpacing = value;
                InvalidateAncestorLayout();
            }
        }

        private FourSide padding;
        public FourSide Padding
        {
            get { return padding; }
            set
            {
                if (padding == value) return;
                padding = value;
                InvalidateAncestorLayout();
            }
        }

        private bool fitChildren;
        public bool FitChildren
        {
            get { return fitChildren; }
            set
            {
                if (fitChildren == value) return;
                fitChildren = value;
                InvalidateLayout();
            }
        }

        private bool fill;
        public bool Fill
        {
            get { return fill; }
            set
            {
                if (fill == value) return;
                fill = value;
                InvalidateLayout();
            }
        }

        public FlowLayout(WidgetManager manager) : base(manager)
        {
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<LinearLayoutStyle>(StyleName);

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var layoutStyle = (LinearLayoutStyle)style;

            //Spacing = layoutStyle.Spacing;
            LineSpacing = layoutStyle.Spacing / 2;
        }

        public override void InvalidateLayout()
        {
            base.InvalidateLayout();
            invalidSizes = true;
        }

        public override void PreLayout()
        {
            base.PreLayout();

            if (NeedsLayout)
            {
                flowWidth = 0;
                InvalidateAncestorLayout();
            }
        }

        protected override void Layout()
        {
            base.Layout();

            measureChildren();
            if (flowWidth != Size.X)
            {
                flowWidth = Size.X;
                InvalidateAncestorLayout();
                return;
            }

            var y = padding.Top;
            foreach (var line in lines)
            {
                var x = padding.Left;
                foreach (var item in line.Items)
                {
                    var child = item.Widget;
                    var minSize = item.MinSize;
                    var maxSize = item.MaxSize;

                    var childHeight = fitChildren ? Math.Max(minSize.Y, line.Height) : Math.Max(minSize.Y, Math.Min(item.PreferredSize.Y, line.Height));
                    if (maxSize.Y > 0 && childHeight > maxSize.Y) childHeight = maxSize.Y;

                    var verticalOffset = 0f;
                    var verticalAlignment = child.AnchorFrom & BoxAlignment.Vertical;
                    switch (verticalAlignment)
                    {
                        case BoxAlignment.Centre: verticalOffset = line.Height * 0.5f; break;
                        case BoxAlignment.Bottom: verticalOffset = line.Height; break;
                    }
                    var anchor = verticalAlignment | BoxAlignment.Left;
                    PlaceChildren(child, new Vector2(x, y + verticalOffset), new Vector2(item.Width, childHeight), anchor);
                    x += item.Width + spacing;
                }
                y += line.Height + lineSpacing;
            }
        }

        protected virtual void PlaceChildren(Widget widget, Vector2 offset, Vector2 size, BoxAlignment anchor)
        {
            widget.Offset = offset;
            widget.Size = size;
            widget.AnchorFrom = anchor;
            widget.AnchorTo = BoxAlignment.TopLeft;
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);

#if DEBUG
            var y = padding.Top;
            foreach (var line in lines)
            {
                var topLeft = AbsolutePosition + new Vector2(padding.Left, y);
                var bottomRight = topLeft + new Vector2(line.GetTotalWidth(spacing), line.Height);
                Manager.Skin.GetDrawable("debug")?.Draw(drawContext, Manager.Camera, new Box2(topLeft, bottomRight), actualOpacity * 0.5f);

                y += line.Height + lineSpacing;
            }
#endif
        }

        private void measureChildren()
        {
            if (!invalidSizes) return;
            invalidSizes = false;

            float width = 0, height = 0;

            // Prepare preferred lengths

            var innerSizeWidth = flowWidth - padding.Horizontal;
            lines = new List<LayoutLine>();
            {
                LayoutLine line = null;
                foreach (var child in Children)
                {
                    if (child.AnchorTarget != null) continue;

                    var preferredSize = child.PreferredSize;
                    var minSize = child.MinSize;
                    var maxSize = child.MaxSize;
                    var itemWidth = preferredSize.X;

                    if (line == null || (flowWidth != 0 && innerSizeWidth < line.GetTotalWidth(spacing) + itemWidth + (line.Items.Count > 0 ? lineSpacing : 0)))
                        lines.Add(line = new LayoutLine());

                    line.Items.Add(new LayoutItem()
                    {
                        Widget = child,
                        PreferredSize = preferredSize,
                        MinSize = minSize,
                        MaxSize = maxSize,
                        Width = itemWidth,
                        Scalable = true,
                    });
                    line.Width += itemWidth;
                    line.Height = Math.Max(line.Height, preferredSize.Y);
                }
            }

            // Distribute the remaining/missing space

            var firstLine = true;
            foreach (var line in lines)
            {
                var scalableItems = line.Items.Count;
                while (scalableItems > 0 && Math.Abs(innerSizeWidth - line.Width) > 0.001f)
                {
                    var remainingWidth = innerSizeWidth - line.Width;
                    if (!fill && remainingWidth > 0) break;

                    var adjustment = remainingWidth / scalableItems;
                    line.Width = line.GetTotalSpacing(spacing);
                    scalableItems = 0;

                    foreach (var item in line.Items)
                    {
                        if (!item.Widget.CanGrow && adjustment > 0)
                            item.Scalable = false;

                        if (item.Scalable)
                        {
                            item.Width += adjustment;
                            if (item.Width < item.MinSize.Y)
                            {
                                item.Width = item.MinSize.Y;
                                item.Scalable = false;
                            }
                            else if (item.MaxSize.Y > 0 && item.Width >= item.MaxSize.Y)
                            {
                                item.Width = item.MaxSize.Y;
                                item.Scalable = false;
                            }
                            else scalableItems++;
                        }
                        line.Width += item.Width;
                    }
                }

                width = Math.Max(width, line.Width);
                height += firstLine ? line.Height : line.Height + lineSpacing;
                firstLine = false;
            }
            flowWidth = Math.Max(flowWidth, width + padding.Horizontal);
            preferredSize = new Vector2(flowWidth, height + padding.Vertical);
        }

        private class LayoutLine
        {
            public List<LayoutItem> Items = new List<LayoutItem>();
            public float Width;
            public float Height;

            public float GetTotalWidth(float spacing) => Width + GetTotalSpacing(spacing);
            public float GetTotalSpacing(float spacing) => spacing * (Items.Count - 1);

            public override string ToString() => $"{Width}x{Height} {Items.Count} Items";
        }

        private class LayoutItem
        {
            public Widget Widget;
            public Vector2 PreferredSize;
            public Vector2 MinSize;
            public Vector2 MaxSize;
            public float Width;
            public bool Scalable;

            public override string ToString() => $"{Widget} Scalable:{Scalable} Length:{Width} PreferredSize:{PreferredSize}";
        }
    }
}
