using OpenTK.Input;
using System;

namespace StorybrewEditor.UserInterface
{
    public class ClickBehavior : IDisposable
    {
        private Widget widget;
        private bool hovered;
        public bool Hovered => !disabled && hovered;
        private bool pressed;
        public bool Pressed => !disabled && pressed;

        private bool disabled;
        public bool Disabled
        {
            get { return disabled; }
            set
            {
                if (disabled == value) return;
                pressed = false;
                disabled = value;
                OnStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private MouseButton pressedButton;

        public event EventHandler OnStateChanged;
        public event EventHandler<MouseButtonEventArgs> OnClick;

        public ClickBehavior(Widget widget)
        {
            this.widget = widget;

            widget.OnHovered += widget_OnHovered;
            widget.OnClickDown += widget_OnClickDown;
            widget.OnClickUp += widget_OnClickUp;
        }

        private void widget_OnHovered(WidgetEvent evt, WidgetHoveredEventArgs e)
        {
            if (hovered == e.Hovered)
                return;

            hovered = e.Hovered;
            if (!disabled) OnStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool widget_OnClickDown(WidgetEvent evt, MouseButtonEventArgs e)
        {
            if (pressed || disabled) return false;

            pressed = true;
            pressedButton = e.Button;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private bool widget_OnClickUp(WidgetEvent evt, MouseButtonEventArgs e)
        {
            if (!pressed || disabled) return false;
            if (e.Button != pressedButton) return false;

            pressed = false;
            if (hovered) OnClick?.Invoke(this, e);
            OnStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    widget.OnHovered -= widget_OnHovered;
                    widget.OnClickDown -= widget_OnClickDown;
                    widget.OnClickUp -= widget_OnClickUp;
                }
                widget = null;
                OnStateChanged = null;
                OnClick = null;

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
