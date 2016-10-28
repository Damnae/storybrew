using OpenTK;
using OpenTK.Input;
using System;

namespace BrewLib.Input
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

        public bool Control { get; private set; }
        public bool Shift { get; private set; }
        public bool Alt { get; private set; }

        public bool ControlOnly => Control && !Shift && !Alt;
        public bool ShiftOnly => !Control && Shift && !Alt;
        public bool AltOnly => !Control && !Shift && Alt;

        public bool ControlShiftOnly => Control && Shift && !Alt;
        public bool ControlAltOnly => Control && !Shift && Alt;
        public bool ShiftAltOnly => !Control && Shift && Alt;

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
        private void window_MouseMove(object sender, MouseMoveEventArgs e) => handler.OnMouseMove(e);

        private void updateModifierState(KeyboardKeyEventArgs e)
        {
            Control = e.Modifiers.HasFlag(KeyModifiers.Control);
            Shift = e.Modifiers.HasFlag(KeyModifiers.Shift);
            Alt = e.Modifiers.HasFlag(KeyModifiers.Alt);
        }
        private void window_KeyDown(object sender, KeyboardKeyEventArgs e) { updateModifierState(e); handler.OnKeyDown(e); }
        private void window_KeyUp(object sender, KeyboardKeyEventArgs e) { updateModifierState(e); handler.OnKeyUp(e); }
        private void window_KeyPress(object sender, KeyPressEventArgs e) => handler.OnKeyPress(e);

        private bool dedupeMouseWheel;
        private void window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (dedupeMouseWheel = !dedupeMouseWheel)
                handler.OnMouseWheel(e);
        }
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
