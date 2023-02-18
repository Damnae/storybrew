using OpenTK.Input;
using System;

namespace BrewLib.UserInterface
{
    public class ClickBehavior : IDisposable
    {
        Widget widget;
        bool hovered;
        public bool Hovered => !disabled && hovered;
        bool pressed;
        public bool Pressed => !disabled && pressed;

        bool disabled;
        public bool Disabled
        {
            get => disabled;
            set
            {
                if (disabled == value) return;
                pressed = false;
                disabled = value;
                OnStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        MouseButton pressedButton;
        public event EventHandler OnStateChanged;
        public event EventHandler<MouseButtonEventArgs> OnClick;

        public ClickBehavior(Widget widget)
        {
            this.widget = widget;

            widget.OnHovered += widget_OnHovered;
            widget.OnClickDown += widget_OnClickDown;
            widget.OnClickUp += widget_OnClickUp;
        }

        void widget_OnHovered(WidgetEvent evt, WidgetHoveredEventArgs e)
        {
            if (hovered == e.Hovered) return;

            hovered = e.Hovered;
            if (!disabled) OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
        bool widget_OnClickDown(WidgetEvent evt, MouseButtonEventArgs e)
        {
            if (pressed || disabled) return false;

            pressed = true;
            pressedButton = e.Button;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        void widget_OnClickUp(WidgetEvent evt, MouseButtonEventArgs e)
        {
            if (!pressed || disabled) return;
            if (e.Button != pressedButton) return;

            pressed = false;
            if (hovered) OnClick?.Invoke(this, e);
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }

        #region IDisposable Support

        bool disposedValue = false;
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
        public void Dispose() => Dispose(true);

        #endregion
    }
}