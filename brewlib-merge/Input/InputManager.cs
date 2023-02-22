using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace BrewLib.Input
{
    public class InputManager : IDisposable
    {
        readonly InputHandler handler;
        readonly GameWindow window;

        bool hadMouseFocus, hasMouseHover;
        public bool HasMouseFocus => window.Focused && hasMouseHover;
        public bool HasWindowFocus => window.Focused;

        readonly Dictionary<int, GamepadManager> gamepadManagers = new Dictionary<int, GamepadManager>();
        public IEnumerable<GamepadManager> GamepadManagers => gamepadManagers.Values;
        public GamepadManager GetGamepadManager(int gamepadIndex = 0) => gamepadManagers[gamepadIndex];

        public Vector2 MousePosition { get; private set; }

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
            foreach (var gamepadIndex in gamepadManagers.Keys) DisableGamepadEvents(gamepadIndex);
            gamepadManagers.Clear();

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
        public void EnableGamepadEvents(int gamepadIndex = 0)
        {
            var manager = new GamepadManager(gamepadIndex);
            manager.OnConnected += gamepadManager_OnConnected;
            manager.OnButtonDown += gamepadManager_OnButtonDown;
            manager.OnButtonUp += gamepadManager_OnButtonUp;
            gamepadManagers.Add(gamepadIndex, manager);
        }
        public void DisableGamepadEvents(int gamepadIndex = 0)
        {
            var manager = gamepadManagers[gamepadIndex];
            manager.OnConnected -= gamepadManager_OnConnected;
            manager.OnButtonDown -= gamepadManager_OnButtonDown;
            manager.OnButtonUp -= gamepadManager_OnButtonUp;
            gamepadManagers.Remove(gamepadIndex);
        }
        public void Update()
        {
            foreach (var gamepadManager in gamepadManagers.Values) gamepadManager.Update();
        }
        void updateMouseFocus()
        {
            if (hadMouseFocus != HasMouseFocus) hadMouseFocus = HasMouseFocus;
            handler.OnFocusChanged(new FocusChangedEventArgs(HasMouseFocus));
        }
        void window_MouseEnter(object sender, EventArgs e)
        {
            hasMouseHover = true;
            updateMouseFocus();
        }
        void window_MouseLeave(object sender, EventArgs e)
        {
            // https://github.com/opentk/opentk/issues/301
            return;

            // hasMouseHover = false;
            // updateMouseFocus();
        }
        void window_FocusedChanged(object sender, EventArgs e) => updateMouseFocus();

        void window_MouseDown(object sender, MouseButtonEventArgs e) => handler.OnClickDown(e);
        void window_MouseUp(object sender, MouseButtonEventArgs e) => handler.OnClickUp(e);
        void window_MouseMove(object sender, MouseMoveEventArgs e)
        {
            MousePosition = new Vector2(e.X, e.Y);
            handler.OnMouseMove(e);
        }

        void updateModifierState(KeyboardKeyEventArgs e)
        {
            Control = e.Modifiers.HasFlag(KeyModifiers.Control);
            Shift = e.Modifiers.HasFlag(KeyModifiers.Shift);
            Alt = e.Modifiers.HasFlag(KeyModifiers.Alt);
        }
        void window_KeyDown(object sender, KeyboardKeyEventArgs e) { updateModifierState(e); handler.OnKeyDown(e); }
        void window_KeyUp(object sender, KeyboardKeyEventArgs e) { updateModifierState(e); handler.OnKeyUp(e); }
        void window_KeyPress(object sender, KeyPressEventArgs e) => handler.OnKeyPress(e);

        bool dedupeMouseWheel;
        void window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (dedupeMouseWheel = !dedupeMouseWheel) handler.OnMouseWheel(e);
        }

        void gamepadManager_OnConnected(object sender, GamepadEventArgs e) => handler.OnGamepadConnected(e);
        void gamepadManager_OnButtonDown(object sender, GamepadButtonEventArgs e) => handler.OnGamepadButtonDown(e);
        void gamepadManager_OnButtonUp(object sender, GamepadButtonEventArgs e) => handler.OnGamepadButtonUp(e);
    }
    public class FocusChangedEventArgs : EventArgs
    {
        readonly bool hasFocus;
        public bool HasFocus => hasFocus;

        public FocusChangedEventArgs(bool hasFocus) => this.hasFocus = hasFocus;
    }
}