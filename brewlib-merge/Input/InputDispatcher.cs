using OpenTK;
using OpenTK.Input;
using System.Collections.Generic;

namespace BrewLib.Input
{
    public class InputDispatcher : InputHandler
    {
        readonly List<InputHandler> handlers = new List<InputHandler>();

        public InputDispatcher() { }
        public InputDispatcher(InputHandler[] handlers)
        {
            foreach (var handler in handlers) this.handlers.Add(handler);
        }

        public void Add(InputHandler handler) => handlers.Add(handler);
        public void Remove(InputHandler handler) => handlers.Remove(handler);
        public void Clear() => handlers.Clear();

        public void OnFocusChanged(FocusChangedEventArgs e)
        {
            foreach (var handler in handlers) handler.OnFocusChanged(e);
        }
        public bool OnClickDown(MouseButtonEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnClickDown(e)) return true;
            return false;
        }
        public bool OnClickUp(MouseButtonEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnClickUp(e)) return true;
            return false;
        }
        public bool OnMouseWheel(MouseWheelEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnMouseWheel(e)) return true;
            return false;
        }
        public void OnMouseMove(MouseMoveEventArgs e)
        {
            foreach (var handler in handlers) handler.OnMouseMove(e);
        }
        public bool OnKeyDown(KeyboardKeyEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnKeyDown(e)) return true;
            return false;
        }
        public bool OnKeyUp(KeyboardKeyEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnKeyUp(e)) return true;
            return false;
        }
        public bool OnKeyPress(KeyPressEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnKeyPress(e)) return true;
            return false;
        }
        public virtual void OnGamepadConnected(GamepadEventArgs e)
        {
            foreach (var handler in handlers) handler.OnGamepadConnected(e);
        }
        public virtual bool OnGamepadButtonDown(GamepadButtonEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnGamepadButtonDown(e)) return true;
            return false;
        }
        public virtual bool OnGamepadButtonUp(GamepadButtonEventArgs e)
        {
            foreach (var handler in handlers) if (handler.OnGamepadButtonUp(e)) return true;
            return false;
        }
    }
}