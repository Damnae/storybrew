using System;

namespace BrewLib.Util
{
    public sealed class ActionDisposable : IDisposable
    {
        private Action action;

        public ActionDisposable(Action action)
        {
            this.action = action;
        }

        public void Dispose() => action();
    }
}
