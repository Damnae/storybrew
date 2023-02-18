using BrewLib.Graphics;
using BrewLib.Input;
using OpenTK.Input;
using System;

namespace BrewLib.ScreenLayers
{
    public abstract class ScreenLayer : InputAdapter, IDisposable
    {
        public ScreenLayerManager Manager { get; set; }

        protected double TransitionInDuration = .25;
        protected double TransitionOutDuration = .25;
        protected double TransitionProgress = 0;
        public double MinTween;

        public State CurrentState = State.Hidden;

        bool hasStarted, hasFocus;
        public bool HasFocus => hasFocus;

        public virtual bool IsPopup => false;
        public bool IsActive => hasFocus && (CurrentState == State.FadingIn || CurrentState == State.Active);

        bool isExiting;
        public bool IsExiting => isExiting;

        readonly InputDispatcher inputDispatcher = new InputDispatcher();
        readonly InputDispatcher innerInputDispatcher = new InputDispatcher();
        public InputHandler InputHandler => inputDispatcher;

        public virtual void Load()
        {
            inputDispatcher.Add(innerInputDispatcher);
            inputDispatcher.Add(this);
        }

        public virtual void GainFocus() => hasFocus = true;
        public virtual void LoseFocus() => hasFocus = false;

        protected void AddInputHandler(InputHandler handler) => innerInputDispatcher.Add(handler);
        protected void RemoveInputHandler(InputHandler handler) => innerInputDispatcher.Remove(handler);
        protected void ClearInputHandlers() => innerInputDispatcher.Clear();

        public virtual void Resize(int width, int height) { }

        public virtual void Update(bool isTopFocus, bool isCovered)
        {
            if (!hasStarted && !isExiting && !isCovered)
            {
                OnStart();
                hasStarted = true;
            }
            if (isExiting)
            {
                if (CurrentState != State.FadingOut) OnTransitionOut();

                CurrentState = State.FadingOut;
                if (!updateTransition(Manager.TimeSource.Elapsed, TransitionOutDuration, -1))
                {
                    OnHidden();
                    Manager.Remove(this);
                }
            }
            else if (isCovered)
            {
                if (updateTransition(Manager.TimeSource.Elapsed, TransitionOutDuration, -1))
                {
                    if (CurrentState != State.FadingOut) OnTransitionOut();
                    CurrentState = State.FadingOut;

                }
                else
                {
                    if (CurrentState != State.Hidden) OnHidden();
                    CurrentState = State.Hidden;
                }
            }
            else
            {
                if (updateTransition(Manager.TimeSource.Elapsed, TransitionInDuration, 1))
                {
                    if (CurrentState != State.FadingIn) OnTransitionIn();
                    CurrentState = State.FadingIn;
                }
                else
                {
                    if (CurrentState != State.Active) OnActive();
                    CurrentState = State.Active;
                }
            }
        }
        public virtual void FixedUpdate() { }
        public virtual void Draw(DrawContext drawContext, double tween) { }

        public virtual void OnStart() { }
        public virtual void OnTransitionIn() { }
        public virtual void OnTransitionOut() { }
        public virtual void OnActive() { }
        public virtual void OnHidden() { }
        public virtual void OnExit() { }

        public virtual void Close() => Exit();

        public override bool OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return true;
            }
            return base.OnKeyDown(e);
        }

        public void Exit() => Exit(false);
        public void Exit(bool skipTransition)
        {
            if (isExiting) return;
            isExiting = true;

            OnExit();

            if (skipTransition || TransitionOutDuration == 0) Manager.Remove(this);
        }
        bool updateTransition(double delta, double duration, int direction)
        {
            var progress = duration > 0 ? delta / duration : 1.0;
            TransitionProgress += progress * direction;
            if (TransitionProgress <= 0)
            {
                TransitionProgress = 0;
                return false;
            }
            if (TransitionProgress >= 1)
            {
                TransitionProgress = 1;
                return false;
            }
            return true;
        }

        #region IDisposable Support

        bool disposedValue = false;
        public bool IsDisposed => disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) if (hasFocus) throw new Exception(GetType().Name + " still has focus!");
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion

        public enum State
        {
            Hidden, FadingIn, Active, FadingOut
        }
    }
}