using System;

namespace StorybrewEditor.UserInterface
{
    public interface Field
    {
        object FieldValue { get; set; }
        event EventHandler OnValueChanged;
        event EventHandler OnDisposed;
    }
}
