using OpenTK;
using OpenTK.Input;
using System;

namespace BrewLib.Input
{
    public class GamepadManager
    {
        readonly int gamepadIndex;
        GamePadState state;
        KeyboardState? keyboardState;
        Vector2 thumb, thumbAlt;
        float triggerLeft, triggerRight;
        GamepadButton previousPressedButtons, pressedButtons;

        public int PlayerIndex => gamepadIndex;
        public bool Connected => state.IsConnected;
        public float TriggerLeft => triggerLeft;
        public float TriggerRight => triggerRight;
        public Vector2 Thumb => thumb;
        public Vector2 ThumbAlt => thumbAlt;

        public bool IsDown(GamepadButton button) => (pressedButtons & button) != 0;
        public bool IsPressed(GamepadButton button) => (previousPressedButtons & button) == 0 && (pressedButtons & button) != 0;
        public bool IsReleased(GamepadButton button) => (previousPressedButtons & button) != 0 && (pressedButtons & button) == 0;

        public event EventHandler<GamepadEventArgs> OnConnected;
        public event EventHandler<GamepadButtonEventArgs> OnButtonDown, OnButtonUp;

        public float TriggerInnerDeadzone = .1f, TriggerOuterDeadzone = .1f,
            AxisInnerDeadzone = .3f, AxisOuterDeadzone = .1f;

        public GamepadManager(int gamepadIndex)
        {
            this.gamepadIndex = gamepadIndex;
            state = GamePad.GetState(gamepadIndex);
        }

        public void Update()
        {
            var wasConnected = state.IsConnected;
            previousPressedButtons = pressedButtons;

            state = GamePad.GetState(gamepadIndex);
            keyboardState = gamepadIndex == 0 && !state.IsConnected ? Keyboard.GetState() : (KeyboardState?)null;
            pressedButtons = 0;

            var buttonsState = state.Buttons;
            var dPadState = state.DPad;

            thumb = applyAxisFilters(state.ThumbSticks.Left);
            thumbAlt = applyAxisFilters(state.ThumbSticks.Right);
            triggerLeft = applyTriggerFilters(state.Triggers.Left);
            triggerRight = applyTriggerFilters(state.Triggers.Right);

            updateButton(dPadState.Left, GamepadButton.DPadLeft, Key.F);
            updateButton(dPadState.Up, GamepadButton.DPadUp, Key.T);
            updateButton(dPadState.Right, GamepadButton.DPadRight, Key.H);
            updateButton(dPadState.Down, GamepadButton.DPadDown, Key.G);
            updateAxis(thumb.X, GamepadButton.ThumbLeft, GamepadButton.ThumbRight, Key.A, Key.D);
            updateAxis(thumb.Y, GamepadButton.ThumbDown, GamepadButton.ThumbUp, Key.S, Key.W);
            updateAxis(thumbAlt.X, GamepadButton.ThumbAltLeft, GamepadButton.ThumbAltRight, Key.Left, Key.Right);
            updateAxis(thumbAlt.Y, GamepadButton.ThumbAltDown, GamepadButton.ThumbAltUp, Key.Down, Key.Up);
            updateButton(buttonsState.A, GamepadButton.A, Key.J);
            updateButton(buttonsState.B, GamepadButton.B, Key.K);
            updateButton(buttonsState.X, GamepadButton.X, Key.U);
            updateButton(buttonsState.Y, GamepadButton.Y, Key.I);
            updateButton(buttonsState.LeftShoulder, GamepadButton.LeftShoulder, Key.O);
            updateButton(buttonsState.RightShoulder, GamepadButton.RightShoulder, Key.L);
            updateTrigger(triggerLeft, GamepadButton.LeftTrigger, Key.P);
            updateTrigger(triggerRight, GamepadButton.RightTrigger, Key.Semicolon);
            updateButton(buttonsState.LeftStick, GamepadButton.Thumb, Key.Q);
            updateButton(buttonsState.RightStick, GamepadButton.ThumbAlt, Key.E);
            updateButton(buttonsState.Start, GamepadButton.Start, Key.Enter);
            updateButton(buttonsState.Back, GamepadButton.Select, Key.Escape);
            updateButton(buttonsState.BigButton, GamepadButton.Home, Key.BackSpace);

            if (!state.IsConnected)
            {
                if (IsDown(GamepadButton.ThumbLeft)) thumb.X -= 1;
                if (IsDown(GamepadButton.ThumbRight)) thumb.X += 1;
                if (IsDown(GamepadButton.ThumbDown)) thumb.Y -= 1;
                if (IsDown(GamepadButton.ThumbUp)) thumb.Y += 1;
                var thumbLeftLength = thumb.Length;
                if (thumbLeftLength > 0) thumb /= thumbLeftLength;

                if (IsDown(GamepadButton.ThumbAltLeft)) thumbAlt.X -= 1;
                if (IsDown(GamepadButton.ThumbAltRight)) thumbAlt.X += 1;
                if (IsDown(GamepadButton.ThumbAltDown)) thumbAlt.Y -= 1;
                if (IsDown(GamepadButton.ThumbAltUp)) thumbAlt.Y += 1;
                var thumbRightLength = thumbAlt.Length;
                if (thumbRightLength > 0) thumbAlt /= thumbRightLength;

                triggerLeft = IsDown(GamepadButton.LeftTrigger) ? 1 : 0;
                triggerRight = IsDown(GamepadButton.RightTrigger) ? 1 : 0;
            }

            if (wasConnected != state.IsConnected) OnConnected?.Invoke(this, new GamepadEventArgs(this));

            var changedButtons = pressedButtons ^ previousPressedButtons;
            for (var button = 1; button <= (int)changedButtons; button <<= 1)
            {
                if (((int)changedButtons & button) == 0) continue;

                if (((int)pressedButtons & button) != 0) OnButtonDown?.Invoke(this, new GamepadButtonEventArgs(this, (GamepadButton)button));
                else OnButtonUp?.Invoke(this, new GamepadButtonEventArgs(this, (GamepadButton)button));
            }
        }
        void updateButton(ButtonState state, GamepadButton button, Key key)
        {
            if (state == ButtonState.Pressed || isKeyDown(key)) pressedButtons |= button;
        }
        void updateTrigger(float state, GamepadButton button, Key key)
        {
            if (state > .5f || isKeyDown(key)) pressedButtons |= button;
        }
        void updateAxis(float state, GamepadButton negativeButton, GamepadButton positiveButton, Key negativeKey, Key positiveKey)
        {
            if (state > .5f || isKeyDown(positiveKey)) pressedButtons |= positiveButton;
            if (state < -.5f || isKeyDown(negativeKey)) pressedButtons |= negativeButton;
        }
        float applyTriggerFilters(float value)
        {
            if (value < TriggerInnerDeadzone) return 0;
            var range = 1f - TriggerOuterDeadzone - TriggerInnerDeadzone;
            var normalizedValue = Math.Min(1f, (value - TriggerInnerDeadzone) / range);
            return normalizedValue / value;
        }
        Vector2 applyAxisFilters(Vector2 value)
        {
            var length = value.Length;
            if (length < AxisInnerDeadzone) return Vector2.Zero;
            var range = 1f - AxisOuterDeadzone - AxisInnerDeadzone;
            var normalizedLength = Math.Min(1f, (length - AxisInnerDeadzone) / range);
            var scale = normalizedLength / length;
            return value * scale;
        }
        bool isKeyDown(Key key) => keyboardState?.IsKeyDown(key) ?? false;
    }
    public class GamepadEventArgs : EventArgs
    {
        public GamepadManager Manager { get; set; }
        public GamepadEventArgs(GamepadManager manager) => Manager = manager;
    }
    public class GamepadButtonEventArgs : GamepadEventArgs
    {
        public GamepadButton Button { get; set; }
        public GamepadButtonEventArgs(GamepadManager manager, GamepadButton button) : base(manager) => Button = button;

        public override string ToString() => Button.ToString();
    }
    [Flags]
    public enum GamepadButton
    {
        None = 0,
        DPadLeft = 1 << 0, DPadUp = 1 << 1, DPadRight = 1 << 2, DPadDown = 1 << 3,
        ThumbLeft = 1 << 4, ThumbUp = 1 << 5, ThumbRight = 1 << 6, ThumbDown = 1 << 7,
        ThumbAltLeft = 1 << 8, ThumbAltUp = 1 << 9, ThumbAltRight = 1 << 10, ThumbAltDown = 1 << 11,
        A = 1 << 12, B = 1 << 13, X = 1 << 14, Y = 1 << 15,
        LeftShoulder = 1 << 16, RightShoulder = 1 << 17,
        LeftTrigger = 1 << 18, RightTrigger = 1 << 19,
        Thumb = 1 << 20, ThumbAlt = 1 << 21,
        Start = 1 << 22, Select = 1 << 23,
        Home = 1 << 24
    }
}