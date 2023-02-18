using System;

namespace BrewLib.UserInterface
{
    public class WidgetEvent
    {
        public readonly Widget Target;
        public readonly Widget RelatedTarget;

        public Widget Listener;
        public bool Handled;

        public WidgetEvent(Widget target, Widget relatedTarget)
        {
            Target = target;
            RelatedTarget = relatedTarget;
        }
    }
    public class WidgetHoveredEventArgs : EventArgs
    {
        readonly bool hovered;
        public bool Hovered => hovered;

        public WidgetHoveredEventArgs(bool hovered) => this.hovered = hovered;
    }
    public class WidgetFocusEventArgs : EventArgs
    {
        private readonly bool hasFocus;
        public bool HasFocus => hasFocus;

        public WidgetFocusEventArgs(bool hasFocus) => this.hasFocus = hasFocus;
    }
}