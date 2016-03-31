using OpenTK;
using OpenTK.Input;
using System;

namespace StorybrewEditor.Input
{
    public class InputManager : IDisposable
    {
        private InputHandler handler;
        private GameWindow window;

        private bool hadMouseFocus;
        private bool hasMouseHover;
        public bool HasMouseFocus => window.Focused && hasMouseHover;
        public bool HasWindowFocus => window.Focused;

        public MouseDevice Mouse => window.Mouse;
        public KeyboardDevice Keyboard => window.Keyboard;

        // Helpers
        public Vector2 MousePosition => new Vector2(Mouse.X, Mouse.Y);

        public InputManager(GameWindow window, InputHandler handler)
        {
            this.window = window;
            this.handler = handler;

            window.FocusedChanged += window_FocusedChanged;
            window.MouseEnter += window_MouseEnter;
            window.MouseLeave += window_MouseLeave;

            window.MouseUp += window_MouseUp;
            window.MouseDown += window_MouseDown;
            window.MouseWheel += window_MouseWheel;
            window.MouseMove += window_MouseMove;
            window.KeyDown += window_KeyDown;
            window.KeyUp += window_KeyUp;
            window.KeyPress += window_KeyPress;
        }

        public void Dispose()
        {
            window.FocusedChanged -= window_FocusedChanged;
            window.MouseEnter -= window_MouseEnter;
            window.MouseLeave -= window_MouseLeave;

            window.MouseUp -= window_MouseUp;
            window.MouseDown -= window_MouseDown;
            window.MouseWheel -= window_MouseWheel;
            window.MouseMove += window_MouseMove;
            window.KeyDown -= window_KeyDown;
            window.KeyUp -= window_KeyUp;
            window.KeyPress -= window_KeyPress;
        }

        private void updateMouseFocus()
        {
            if (hadMouseFocus != HasMouseFocus)
                hadMouseFocus = HasMouseFocus;

            handler.OnFocusChanged(new FocusChangedEventArgs(HasMouseFocus));
        }

        private void window_MouseEnter(object sender, EventArgs e)
        {
            hasMouseHover = true;
            updateMouseFocus();
        }

        private void window_MouseLeave(object sender, EventArgs e)
        {
            // https://github.com/opentk/opentk/issues/301
            return;

            hasMouseHover = false;
            updateMouseFocus();
        }

        private void window_FocusedChanged(object sender, EventArgs e) => updateMouseFocus();

        private void window_MouseDown(object sender, MouseButtonEventArgs e) => handler.OnClickDown(e);
        private void window_MouseUp(object sender, MouseButtonEventArgs e) => handler.OnClickUp(e);
        private void window_MouseWheel(object sender, MouseWheelEventArgs e) => handler.OnMouseWheel(e);
        private void window_MouseMove(object sender, MouseMoveEventArgs e) => handler.OnMouseMove(e);
        private void window_KeyDown(object sender, KeyboardKeyEventArgs e) => handler.OnKeyDown(e);
        private void window_KeyUp(object sender, KeyboardKeyEventArgs e) => handler.OnKeyUp(e);
        private void window_KeyPress(object sender, KeyPressEventArgs e) => handler.OnKeyPress(e);
    }

    public class FocusChangedEventArgs : EventArgs
    {
        private bool hasFocus;
        public bool HasFocus => hasFocus;

        public FocusChangedEventArgs(bool hasFocus)
        {
            this.hasFocus = hasFocus;
        }
    }
}
