using OpenTK;
using OpenTK.Input;

namespace BrewLib.Input
{
    public abstract class InputAdapter : InputHandler
    {
        public virtual void OnFocusChanged(FocusChangedEventArgs e) { }
        public virtual bool OnClickDown(MouseButtonEventArgs e) => false;
        public virtual bool OnClickUp(MouseButtonEventArgs e) => false;
        public virtual bool OnMouseWheel(MouseWheelEventArgs e) => false;
        public virtual void OnMouseMove(MouseMoveEventArgs e) { }
        public virtual bool OnKeyDown(KeyboardKeyEventArgs e) => false;
        public virtual bool OnKeyUp(KeyboardKeyEventArgs e) => false;
        public virtual bool OnKeyPress(KeyPressEventArgs e) => false;
        public virtual void OnGamepadConnected(GamepadEventArgs e) { }
        public virtual bool OnGamepadButtonDown(GamepadButtonEventArgs e) => false;
        public virtual bool OnGamepadButtonUp(GamepadButtonEventArgs e) => false;
    }
}