namespace StorybrewCommon.Util
{
    public sealed class Pool<T> where T : class
    {
        private readonly Func<T> allocator;
        private readonly Action<T> disposer;
        private readonly Queue<T> queue;

        public readonly int PoolCapacity;

#if DEBUG
        private int virtualCapacity;

        public int OptimalCapacity { get; private set; }
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
            OptimalCapacity = Math.Max(virtualCapacity, OptimalCapacity);
#endif
            if (queue.Count > PoolCapacity)
                return;

            disposer?.Invoke(obj);
            queue.Enqueue(obj);
        }
    }
}
