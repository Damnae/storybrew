using BrewLib.UserInterface.Skinning.Styles;
using OpenTK;
using System;

namespace BrewLib.UserInterface
{
    public class StackLayout : Widget
    {
        private Vector2 minSize;
        private Vector2 preferredSize;
        private bool invalidSizes = true;

        public override Vector2 MinSize { get { measureChildren(); return minSize; } }
        public override Vector2 PreferredSize { get { measureChildren(); return preferredSize; } }

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

        public StackLayout(WidgetManager manager) : base(manager)
        {
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<StackLayoutStyle>(StyleName);

        public override void InvalidateLayout()
        {
            base.InvalidateLayout();
            invalidSizes = true;
        }

        protected override void Layout()
        {
            base.Layout();

            foreach (var child in Children)
            {
                if (child.AnchorTarget != null) continue;
                if (child.AnchorFrom != child.AnchorTo) continue; // ?

                var preferredSize = child.PreferredSize;
                var minSize = child.MinSize;
                var maxSize = child.MaxSize;

                var childWidth = fitChildren ? Math.Max(minSize.X, Size.X) : Math.Max(minSize.X, Math.Min(preferredSize.X, Size.X));
                if (maxSize.X > 0 && childWidth > maxSize.X) childWidth = maxSize.X;

                var childHeight = fitChildren ? Math.Max(minSize.Y, Size.Y) : Math.Max(minSize.Y, Math.Min(preferredSize.Y, Size.Y));
                if (maxSize.Y > 0 && childHeight > maxSize.Y) childHeight = maxSize.Y;

                PlaceChildren(child, Vector2.Zero, new Vector2(childWidth, childHeight));
            }
        }

        // Override to animate, etc.
        protected virtual void PlaceChildren(Widget widget, Vector2 offset, Vector2 size)
        {
            widget.Size = size;
        }

        private void measureChildren()
        {
            if (!invalidSizes) return;
            invalidSizes = false;

            var width = 0f;
            var height = 0f;

            var minWidth = width;
            var minHeight = height;

            foreach (var child in Children)
            {
                if (child.AnchorTarget != null) continue;

                var childMinSize = child.MinSize;
                var childSize = child.PreferredSize;

                width = Math.Max(width, childSize.X);
                height = Math.Max(height, childSize.Y);

                minWidth = Math.Max(minWidth, childMinSize.X);
                minHeight = Math.Max(minHeight, childMinSize.Y);
            }
            minSize = new Vector2(minWidth, minHeight);
            preferredSize = new Vector2(width, height);
        }
    }
}
