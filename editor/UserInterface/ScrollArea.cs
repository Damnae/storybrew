using OpenTK;
using OpenTK.Input;
using System;
using StorybrewEditor.Graphics;

namespace StorybrewEditor.UserInterface
{
    public class ScrollArea : Widget
    {
        private bool hovered;

        public override Vector2 MinSize => Vector2.Zero;
        public override Vector2 PreferredSize => scrollContainer.PreferredSize;

        private bool scrollsVertically = true;
        public bool ScrollsVertically
        {
            get { return scrollsVertically; }
            set
            {
                if (scrollsVertically == value) return;
                scrollsVertically = value;
                updateScrollIndicators();
            }
        }

        private bool scrollsHorizontally;
        public bool ScrollsHorizontally
        {
            get { return scrollsHorizontally; }
            set
            {
                if (scrollsHorizontally == value) return;
                scrollsHorizontally = value;
                updateScrollIndicators();
            }
        }

        public float ScrollableX => Math.Max(0, scrollContainer.Width - Width);
        public float ScrollableY => Math.Max(0, scrollContainer.Height - Height);

        private StackLayout scrollContainer;
        private Label scrollIndicatorTop;
        private Label scrollIndicatorBottom;
        private Label scrollIndicatorLeft;
        private Label scrollIndicatorRight;
        private bool dragged;

        public ScrollArea(WidgetManager manager, Widget scrollable) : base(manager)
        {
            ClipChildren = true;
            Add(scrollContainer = new StackLayout(manager)
            {
                FitChildren = true,
                Children = new Widget[]
                {
                    scrollable
                },
            });
            Add(scrollIndicatorTop = new Label(manager)
            {
                StyleName = "icon",
                Icon = Util.IconFont.ArrowCircleUp,
                AnchorFrom = UiAlignment.TopRight,
                AnchorTo = UiAlignment.TopRight,
                Hoverable = false,
                Opacity = 0.6f,
            });
            Add(scrollIndicatorBottom = new Label(manager)
            {
                StyleName = "icon",
                Icon = Util.IconFont.ArrowCircleDown,
                AnchorFrom = UiAlignment.BottomRight,
                AnchorTo = UiAlignment.BottomRight,
                Hoverable = false,
                Opacity = 0.6f,
            });
            Add(scrollIndicatorLeft = new Label(manager)
            {
                StyleName = "icon",
                Icon = Util.IconFont.ArrowCircleLeft,
                AnchorFrom = UiAlignment.BottomLeft,
                AnchorTo = UiAlignment.BottomLeft,
                Hoverable = false,
                Opacity = 0.6f,
            });
            Add(scrollIndicatorRight = new Label(manager)
            {
                StyleName = "icon",
                Icon = Util.IconFont.ArrowCircleRight,
                AnchorFrom = UiAlignment.BottomRight,
                AnchorTo = UiAlignment.BottomRight,
                Hoverable = false,
                Opacity = 0.6f,
            });
            OnHovered += (sender, e) =>
            {
                hovered = e.Hovered;
                updateScrollIndicators();
            };
            OnClickDown += (sender, e) =>
            {
                if (e.Button != MouseButton.Left) return false;
                dragged = true;
                return true;
            };
            OnClickUp += (sender, e) =>
            {
                if (e.Button != MouseButton.Left) return;
                dragged = false;
            };
            OnDrag += (sender, e) =>
            {
                if (!dragged) return;
                scroll(e.XDelta, e.YDelta);
            };
            OnMouseWheel += (sender, e) =>
            {
                if (scrollsVertically)
                    scroll(0, e.DeltaPrecise * 64);
                else if (scrollsHorizontally)
                    scroll(e.DeltaPrecise * 64, 0);
                return true;
            };

            scrollIndicatorTop.Pack();
            scrollIndicatorBottom.Pack();
            scrollIndicatorLeft.Pack();
            scrollIndicatorRight.Pack();
            updateScrollIndicators();
        }

        protected override void DrawChildren(DrawContext drawContext, float actualOpacity)
        {
            scroll(0, 0);
            base.DrawChildren(drawContext, actualOpacity);
        }

        protected override void Layout()
        {
            base.Layout();
            var width = scrollsHorizontally ? Math.Max(Size.X, scrollContainer.PreferredSize.X) : Size.X;
            var height = scrollsVertically ? Math.Max(Size.Y, scrollContainer.PreferredSize.Y) : Size.Y;
            scrollContainer.Size = new Vector2(width, height);
        }

        private void scroll(float x, float y)
        {
            if (!scrollsHorizontally) x = 0;
            if (!scrollsVertically) y = 0;

            scrollContainer.Offset = new Vector2(
                Math.Max(-ScrollableX, Math.Min(scrollContainer.Offset.X + x, 0)),
                Math.Max(-ScrollableY, Math.Min(scrollContainer.Offset.Y + y, 0))
            );
            updateScrollIndicators();
        }

        private void updateScrollIndicators()
        {
            scrollIndicatorTop.Displayed = hovered && scrollsVertically && scrollContainer.Offset.Y < 0;
            scrollIndicatorBottom.Displayed = hovered && scrollsVertically && scrollContainer.Offset.Y > -ScrollableY;
            scrollIndicatorLeft.Displayed = hovered && scrollsHorizontally && scrollContainer.Offset.X < 0;
            scrollIndicatorRight.Displayed = hovered && scrollsHorizontally && scrollContainer.Offset.X > -ScrollableX;

            scrollIndicatorBottom.Offset = scrollIndicatorRight.Displayed ? new Vector2(0, -scrollIndicatorRight.Height) : Vector2.Zero;
            scrollIndicatorRight.Offset = scrollIndicatorBottom.Displayed ? new Vector2(-scrollIndicatorBottom.Width, 0) : Vector2.Zero;
        }
    }
}
