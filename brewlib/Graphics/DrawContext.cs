using System;
using System.Collections.Generic;

namespace BrewLib.Graphics
{
    public class DrawContext : IDisposable
    {
        private Dictionary<Type, object> references = new Dictionary<Type, object>();
        private List<IDisposable> disposables = new List<IDisposable>();

        public T Get<T>() => (T)references[typeof(T)];

        public void Register<T>(T obj, bool dispose = false)
        {
            references.Add(typeof(T), obj);

            var disposable = obj as IDisposable;
            if (dispose && disposable != null)
                disposables.Add(disposable);
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var disposable in disposables)
                        disposable.Dispose();
                }
                references = null;
                disposables = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
