using System;
using System.Collections.Generic;

namespace StorybrewCommon.Util
{
    public sealed class Pool<T> where T : class
    {
        private Func<T> allocator;
        private Action<T> disposer;
        private Queue<T> queue;

        public readonly int PoolCapacity;

#if DEBUG
        private int virtualCapacity;
        private int optimalCapacity;
        public int OptimalCapacity => optimalCapacity;
#endif

        public Pool(Func<T> allocator, Action<T> disposer = null, int poolCapacity = 256)
        {
            this.allocator = allocator;
            this.disposer = disposer;

            PoolCapacity = poolCapacity;
            queue = new Queue<T>(poolCapacity);
        }

        public void PreAllocate()
        {
            while (queue.Count < PoolCapacity)
                queue.Enqueue(allocator());
        }

        public T Retrieve()
        {
#if DEBUG
            --virtualCapacity;
            virtualCapacity = Math.Max(0, virtualCapacity);
#endif
            if (queue.Count == 0)
                return allocator();

            return queue.Dequeue();
        }

        public void Release(IEnumerable<T> objects)
        {
            foreach (var obj in objects)
                Release(obj);
        }

        public void Release(T obj)
        {
#if DEBUG
            ++virtualCapacity;
            optimalCapacity = Math.Max(virtualCapacity, optimalCapacity);
#endif
            if (queue.Count > PoolCapacity)
                return;

            disposer?.Invoke(obj);
            queue.Enqueue(obj);
        }
    }
}
