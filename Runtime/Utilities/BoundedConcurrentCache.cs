using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Minerva.Localizations.Utilities
{
    /// <summary>
    /// A concurrent lookup cache with FIFO eviction and lock-free hits.
    /// </summary>
    internal sealed class BoundedConcurrentCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> entries = new();
        private readonly Queue<TKey> insertionOrder;
        private readonly object insertionLock = new();
        private readonly int capacity;

        public int Count => entries.Count;

        public BoundedConcurrentCache(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            insertionOrder = new Queue<TKey>();
        }

        public bool TryGetValue(TKey key, out TValue value) => entries.TryGetValue(key, out value);

        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (entries.TryGetValue(key, out var existing)) return existing;

            lock (insertionLock)
            {
                if (entries.TryGetValue(key, out existing)) return existing;

                while (entries.Count >= capacity)
                {
                    TKey oldest = insertionOrder.Dequeue();
                    entries.TryRemove(oldest, out _);
                }

                insertionOrder.Enqueue(key);
                entries.TryAdd(key, value);
                return value;
            }
        }

        public void Clear()
        {
            lock (insertionLock)
            {
                entries.Clear();
                insertionOrder.Clear();
            }
        }
    }
}
