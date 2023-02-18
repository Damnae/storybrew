using OpenTK;
using OpenTK.Input;

namespace BrewLib.Input
{
    public interface InputHandler
    {
        void OnFocusChanged(FocusChangedEventArgs e);
        bool OnClickDown(MouseButtonEventArgs e);
        bool OnClickUp(MouseButtonEventArgs e);
        bool OnMouseWheel(MouseWheelEventArgs e);
        void OnMouseMove(MouseMoveEventArgs e);
        bool OnKeyDown(KeyboardKeyEventArgs e);
        bool OnKeyUp(KeyboardKeyEventArgs e);
        bool OnKeyPress(KeyPressEventArgs e);
        void OnGamepadConnected(GamepadEventArgs e);
        bool OnGamepadButtonDown(GamepadButtonEventArgs e);
        bool OnGamepadButtonUp(GamepadButtonEventArgs e);
    }
}