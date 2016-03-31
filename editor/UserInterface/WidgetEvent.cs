using System;

namespace StorybrewEditor.UserInterface
{
    public class WidgetEvent
    {
        public readonly Widget Target;
        public readonly Widget RelatedTarget;

        /// <summary>
        /// The widget that handled the event, or the last widget that received it.
        /// </summary>
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
        private bool hovered;
        public bool Hovered => hovered;

        public WidgetHoveredEventArgs(bool hovered)
        {
            this.hovered = hovered;
        }
    }

    public class WidgetFocusEventArgs : EventArgs
    {
        private bool hasFocus;
        public bool HasFocus => hasFocus;

        public WidgetFocusEventArgs(bool hasFocus)
        {
            this.hasFocus = hasFocus;
        }
    }
}
