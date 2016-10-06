using OpenTK;
using OpenTK.Input;
using System;

namespace StorybrewEditor.UserInterface
{
    public class ScrollArea : Widget
    {
        public override Vector2 MinSize => Vector2.Zero;
        public override Vector2 PreferredSize => scrollContainer.PreferredSize;

        public bool ScrollsVertically = true;
        public bool ScrollsHorizontally = true;

        private StackLayout scrollContainer;
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
                if (ScrollsVertically)
                    scroll(0, e.DeltaPrecise * 64);
                else if (ScrollsHorizontally)
                    scroll(e.DeltaPrecise * 64, 0);
                return true;
            };
        }

        protected override void Layout()
        {
            base.Layout();
            var width = ScrollsHorizontally ? Math.Max(Size.X, scrollContainer.PreferredSize.X) : Size.X;
            var height = ScrollsVertically ? Math.Max(Size.Y, scrollContainer.PreferredSize.Y) : Size.Y;
            scrollContainer.Size = new Vector2(width, height);
            scroll(0, 0);
        }

        private void scroll(float x, float y)
        {
            if (!ScrollsHorizontally) x = 0;
            if (!ScrollsVertically) y = 0;

            var scrollableX = Math.Max(0, scrollContainer.Width - Width);
            var scrollableY = Math.Max(0, scrollContainer.Height - Height);
            scrollContainer.Offset = new Vector2(
                Math.Max(-scrollableX, Math.Min(scrollContainer.Offset.X + x, 0)), 
                Math.Max(-scrollableY, Math.Min(scrollContainer.Offset.Y + y, 0))
            );
        }
    }
}
