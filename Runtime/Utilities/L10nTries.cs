using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Minerva.Localizations
{
    public readonly struct TriesSegment<TValue>
    {
        private readonly L10nTriesNode<TValue> root;

        public bool HasValue => root != null;
        public int Count => root?.Count ?? 0;

        public L10nTries<TValue>.FirstLayerKeyCollection FirstLayerKeys => root?.FirstLayerKeys ?? L10nTries<TValue>.FirstLayerKeyCollection.Empty;
        public L10nTries<TValue>.KeyCollection Keys => new L10nTries<TValue>.KeyCollection(root);

        internal TriesSegment(L10nTriesNode<TValue> root)
        {
            this.root = root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out TValue value)
        {
            if (root == null)
            {
                value = default;
                return false;
            }

            return root.TryGetValue(key.AsMemory(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(ReadOnlyMemory<char> key, out TValue value)
        {
            if (root == null)
            {
                value = default;
                return false;
            }

            return root.TryGetValue(key, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<TKey>(in TKey key, out TValue value) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (root == null)
            {
                value = default;
                return false;
            }

            return root.TryGetValue(in key, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => TryGetValue(key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(ReadOnlyMemory<char> key) => TryGetValue(key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey<TKey>(in TKey key) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>> => TryGetValue(in key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSegment(string partialKey, out TriesSegment<TValue> segment)
        {
            if (root == null)
            {
                segment = default;
                return false;
            }

            return TryGetSegment(partialKey.AsMemory(), out segment);
        }

        public bool TryGetSegment(ReadOnlyMemory<char> partialKey, out TriesSegment<TValue> segment)
        {
            if (root == null)
            {
                segment = default;
                return false;
            }

            if (!root.TryGetSegment(partialKey, out var segmentRoot))
            {
                segment = default;
                return false;
            }

            segment = new TriesSegment<TValue>(segmentRoot);
            return true;
        }

        public bool TryGetSegment<TKey>(in TKey partialKey, out TriesSegment<TValue> segment) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (root == null)
            {
                segment = default;
                return false;
            }

            if (!root.TryGetSegment(in partialKey, out var segmentRoot))
            {
                segment = default;
                return false;
            }

            segment = new TriesSegment<TValue>(segmentRoot);
            return true;
        }

        public bool CopyFirstLayerKeys(string partialKey, List<string> output)
        {
            if (root == null)
            {
                output.Clear();
                return false;
            }

            return CopyFirstLayerKeys(partialKey.AsMemory(), output);
        }

        public bool CopyFirstLayerKeys(ReadOnlyMemory<char> partialKey, List<string> output)
        {
            if (root == null)
            {
                output.Clear();
                return false;
            }

            if (!root.TryGetSegment(partialKey, out var segRoot))
            {
                output.Clear();
                return false;
            }

            return segRoot.CopyFirstLayerKeys(output);
        }

        public bool CopyFirstLayerKeys<TKey>(in TKey partialKey, List<string> output) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (root == null)
            {
                output.Clear();
                return false;
            }

            if (!root.TryGetSegment(in partialKey, out var segRoot))
            {
                output.Clear();
                return false;
            }

            return segRoot.CopyFirstLayerKeys(output);
        }
    }

    internal sealed class MemoryCharsComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly MemoryCharsComparer Default = new MemoryCharsComparer();

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.SequenceEqual(y.Span);

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
            var hash = new HashCode();
            var span = obj.Span;
            for (int i = 0; i < span.Length; i++)
            {
                hash.Add(span[i]);
            }
            return hash.ToHashCode();
        }
    }

    internal sealed class L10nTriesNode<TValue>
    {
        private bool isTerminated;
        private TValue value;
        private Dictionary<ReadOnlyMemory<char>, L10nTriesNode<TValue>> children;
        private int count;

        public Dictionary<ReadOnlyMemory<char>, L10nTriesNode<TValue>> Children => children ??= new Dictionary<ReadOnlyMemory<char>, L10nTriesNode<TValue>>(MemoryCharsComparer.Default);

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count;
        }

        public TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => value;
        }

        public bool IsTerminated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => isTerminated;
        }

        public L10nTries<TValue>.FirstLayerKeyCollection FirstLayerKeys => new L10nTries<TValue>.FirstLayerKeyCollection(this);

        internal void Clear()
        {
            isTerminated = false;
            value = default;
            children?.Clear();
            count = 0;
        }

        public void Set(string key, TValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                bool wasTerminated = isTerminated;
                this.value = value;
                isTerminated = true;
                if (!wasTerminated) count++;
                return;
            }

            int segmentCount = 1;
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] == '.') segmentCount++;
            }

            int pathCapacity = segmentCount + 1;

            var pool = ArrayPool<L10nTriesNode<TValue>>.Shared;
            L10nTriesNode<TValue>[] path = pool.Rent(pathCapacity);

            int pathLen = 0;

            try
            {
                var node = this;
                path[pathLen++] = node;

                int start = 0;
                for (int i = 0; i <= key.Length; i++)
                {
                    if (i == key.Length || key[i] == '.')
                    {
                        var seg = key.AsMemory(start, i - start);

                        if (!node.Children.TryGetValue(seg, out var child))
                        {
                            child = new L10nTriesNode<TValue>();
                            node.Children.Add(seg, child);
                        }

                        node = child;
                        path[pathLen++] = node;

                        start = i + 1;
                    }
                }

                bool was = node.isTerminated;
                node.value = value;
                node.isTerminated = true;

                if (!was)
                {
                    for (int i = 0; i < pathLen; i++)
                    {
                        path[i].count++;
                    }
                }
            }
            finally
            {
                // 里面是 Node 引用，不需要 clearArray
                pool.Return(path, clearArray: false);
            }
        }

        public bool Remove(string key)
        {
            return Remove(key.AsMemory());
        }

        public bool Remove(ReadOnlyMemory<char> key)
        {
            if (key.IsEmpty)
            {
                if (!isTerminated) return false;

                isTerminated = false;
                value = default;
                count--;
                return true;
            }

            int segmentCount = 1;
            var span = key.Span;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '.') segmentCount++;
            }

            int nodePathCapacity = segmentCount + 1;
            int keyPathCapacity = segmentCount;

            var nodePool = ArrayPool<L10nTriesNode<TValue>>.Shared;
            var keyPool = ArrayPool<ReadOnlyMemory<char>>.Shared;

            L10nTriesNode<TValue>[] nodePath = nodePool.Rent(nodePathCapacity);
            ReadOnlyMemory<char>[] keyPath = keyPool.Rent(keyPathCapacity);

            int nodeLen = 0;
            int keyLen = 0;

            try
            {
                var node = this;
                nodePath[nodeLen++] = node;

                int start = 0;
                for (int i = 0; i <= span.Length; i++)
                {
                    if (i == span.Length || span[i] == '.')
                    {
                        var seg = key.Slice(start, i - start);

                        if (node.children == null || !node.children.TryGetValue(seg, out node))
                        {
                            return false;
                        }

                        keyPath[keyLen++] = seg;
                        nodePath[nodeLen++] = node;

                        start = i + 1;
                    }
                }

                if (!node.isTerminated) return false;

                node.isTerminated = false;
                node.value = default;

                for (int i = 0; i < nodeLen; i++)
                {
                    nodePath[i].count--;
                }

                for (int i = nodeLen - 1; i >= 1; i--)
                {
                    var current = nodePath[i];
                    if (current.count > 0) break;

                    if (current.children != null && current.children.Count > 0)
                    {
                        break;
                    }

                    var parent = nodePath[i - 1];
                    if (parent.children == null) break;

                    parent.children.Remove(keyPath[i - 1]);
                }

                return true;
            }
            finally
            {
                nodePool.Return(nodePath, clearArray: false);

                // ReadOnlyMemory<char> 里会引用 string；清理避免 string 被池“挂住”
                keyPool.Return(keyPath, clearArray: true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetSelfValue(out TValue value)
        {
            if (IsTerminated)
            {
                value = Value;
                return true;
            }

            value = default;
            return false;
        }

        internal bool TryGetValue(ReadOnlyMemory<char> key, out TValue value)
        {
            if (key.IsEmpty)
            {
                return TryGetSelfValue(out value);
            }

            var node = this;
            var span = key.Span;

            int start = 0;
            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == '.')
                {
                    var seg = key.Slice(start, i - start);

                    if (node.children == null || !node.children.TryGetValue(seg, out node))
                    {
                        value = default;
                        return false;
                    }

                    start = i + 1;
                }
            }

            return node.TryGetSelfValue(out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetValue(string key, out TValue value)
        {
            return TryGetValue(key.AsMemory(), out value);
        }

        internal bool TryGetValue<TKey>(in TKey key, out TValue value) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (key.Count == 0)
            {
                return TryGetSelfValue(out value);
            }

            var node = this;

            for (int i = 0; i < key.Count; i++)
            {
                var seg = key[i];
                if (node.children == null || !node.children.TryGetValue(seg, out node))
                {
                    value = default;
                    return false;
                }
            }

            return node.TryGetSelfValue(out value);
        }

        internal bool TryGetSegment(ReadOnlyMemory<char> partialKey, out L10nTriesNode<TValue> segmentRoot)
        {
            if (partialKey.IsEmpty)
            {
                segmentRoot = this;
                return true;
            }

            var node = this;
            var span = partialKey.Span;

            int start = 0;
            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == '.')
                {
                    var seg = partialKey.Slice(start, i - start);

                    if (node.children == null || !node.children.TryGetValue(seg, out node))
                    {
                        segmentRoot = null;
                        return false;
                    }

                    start = i + 1;
                }
            }

            segmentRoot = node;
            return true;
        }

        internal bool TryGetSegment<TKey>(in TKey partialKey, out L10nTriesNode<TValue> segmentRoot) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (partialKey.Count == 0)
            {
                segmentRoot = this;
                return true;
            }

            var node = this;

            for (int i = 0; i < partialKey.Count; i++)
            {
                var seg = partialKey[i];
                if (node.children == null || !node.children.TryGetValue(seg, out node))
                {
                    segmentRoot = null;
                    return false;
                }
            }

            segmentRoot = node;
            return true;
        }

        internal bool CopyFirstLayerKeys(List<string> output)
        {
            output.Clear();

            if (children == null || children.Count == 0)
            {
                return true;
            }

            foreach (var kv in children)
            {
                output.Add(kv.Key.ToString());
            }

            return true;
        }

        internal void TraverseCopyKey(StringBuilder builder, char separator, IList<string> array, ref int index)
        {
            if (isTerminated)
            {
                array[index++] = builder.ToString();
                if (count <= 1) return;
            }

            if (children == null || children.Count == 0) return;

            int baseLen = builder.Length;
            if (baseLen > 0) builder.Append(separator);

            foreach (var kv in children)
            {
                int before = builder.Length;
                builder.Append(kv.Key.Span);
                kv.Value.TraverseCopyKey(builder, separator, array, ref index);
                builder.Length = before;
            }

            builder.Length = baseLen;
        }
    }

    public sealed class L10nTries<TValue> : IDictionary<string, TValue>
    {
        public readonly struct FirstLayerKeyCollection : ICollection<string>, IEnumerable<string>, IEnumerable, IReadOnlyCollection<string>
        {
            private readonly Dictionary<ReadOnlyMemory<char>, L10nTriesNode<TValue>>.KeyCollection collection;

            public static FirstLayerKeyCollection Empty { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new FirstLayerKeyCollection(); }

            public int Count => collection?.Count ?? 0;
            public bool IsReadOnly => true;

            internal FirstLayerKeyCollection(L10nTriesNode<TValue> node)
            {
                collection = node.Children.Keys;
            }

            public bool Contains(string item)
            {
                if (collection == null) return false;
                return ((ICollection<ReadOnlyMemory<char>>)collection).Contains(item.AsMemory());
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                if (collection == null) return;
                foreach (var mem in collection)
                {
                    array[arrayIndex++] = mem.ToString();
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                if (collection == null) yield break;
                foreach (var mem in collection)
                {
                    yield return mem.ToString();
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();

            public string[] ToArray()
            {
                if (collection == null) return Array.Empty<string>();
                var arr = new string[collection.Count];
                CopyTo(arr, 0);
                return arr;
            }
        }

        public readonly struct KeyCollection : ICollection<string>, IEnumerable<string>, IEnumerable, IReadOnlyCollection<string>
        {
            private readonly L10nTriesNode<TValue> root;
            private readonly char separator;

            public int Count => root?.Count ?? 0;
            public bool IsReadOnly => true;

            internal KeyCollection(L10nTriesNode<TValue> root, char separator = '.')
            {
                this.root = root;
                this.separator = separator;
            }

            public bool Contains(string item)
            {
                if (root == null) return false;
                return root.TryGetValue(item, out _);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                if (root == null) return;

                int idx = arrayIndex;
                root.TraverseCopyKey(new StringBuilder(), separator, array, ref idx);
            }

            public IEnumerator<string> GetEnumerator()
            {
                if (root == null) yield break;

                var list = new string[Count];
                CopyTo(list, 0);
                for (int i = 0; i < list.Length; i++)
                {
                    yield return list[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();

            public string[] ToArray()
            {
                if (root == null) return Array.Empty<string>();
                var arr = new string[Count];
                CopyTo(arr, 0);
                return arr;
            }
        }

        public readonly struct ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable, IReadOnlyCollection<TValue>
        {
            private readonly L10nTries<TValue> owner;

            public int Count => owner?.Count ?? 0;
            public bool IsReadOnly => true;

            internal ValueCollection(L10nTries<TValue> owner)
            {
                this.owner = owner;
            }

            public bool Contains(TValue item)
            {
                if (owner == null) return false;

                foreach (var kv in (IEnumerable<KeyValuePair<string, TValue>>)owner)
                {
                    if (EqualityComparer<TValue>.Default.Equals(kv.Value, item))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array));
                if (owner == null) return;
                if ((uint)arrayIndex > (uint)array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (array.Length - arrayIndex < Count) throw new ArgumentException("Insufficient array length.");

                int i = arrayIndex;
                foreach (var kv in (IEnumerable<KeyValuePair<string, TValue>>)owner)
                {
                    array[i++] = kv.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                if (owner == null) yield break;

                foreach (var kv in (IEnumerable<KeyValuePair<string, TValue>>)owner)
                {
                    yield return kv.Value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();
            void ICollection<TValue>.Clear() => throw new NotSupportedException();
            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();
        }

        private readonly L10nTriesNode<TValue> root = new L10nTriesNode<TValue>();

        public L10nTries() { }

        public L10nTries(IDictionary<string, TValue> dictionary)
        {
            foreach (var kv in dictionary)
            {
                Set(kv.Key, kv.Value);
            }
        }

        public int Count => root.Count;

        public FirstLayerKeyCollection FirstLayerKeys => root.FirstLayerKeys;

        public KeyCollection Keys => new KeyCollection(root);

        public ValueCollection Values => new ValueCollection(this);

        public TriesSegment<TValue> RootSegment => new TriesSegment<TValue>(root);

        public TValue this[string key]
        {
            get
            {
                if (!root.TryGetValue(key.AsMemory(), out var value))
                {
                    throw new KeyNotFoundException($"Key '{key}' not found in L10nTries.");
                }
                return value;
            }
            set => Set(key, value);
        }

        public void Clear() => root.Clear();

        public void Set(string key, TValue value) => root.Set(key, value);

        public void Add(string key, TValue value)
        {
            if (root.TryGetValue(key.AsMemory(), out _))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));
            }

            Set(key, value);
        }

        public bool Remove(string key) => root.Remove(key.AsMemory());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out TValue value) => root.TryGetValue(key.AsMemory(), out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(ReadOnlyMemory<char> key, out TValue value) => root.TryGetValue(key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<TKey>(in TKey key, out TValue value) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>> => root.TryGetValue(in key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => root.TryGetValue(key.AsMemory(), out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(ReadOnlyMemory<char> key) => root.TryGetValue(key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey<TKey>(in TKey key) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>> => root.TryGetValue(in key, out _);

        public bool TryGetSegment(string partialKey, out TriesSegment<TValue> segment)
        {
            return TryGetSegment(partialKey.AsMemory(), out segment);
        }

        public bool TryGetSegment(ReadOnlyMemory<char> partialKey, out TriesSegment<TValue> segment)
        {
            if (!root.TryGetSegment(partialKey, out var segmentRoot))
            {
                segment = default;
                return false;
            }

            segment = new TriesSegment<TValue>(segmentRoot);
            return true;
        }

        public bool TryGetSegment<TKey>(in TKey partialKey, out TriesSegment<TValue> segment) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (!root.TryGetSegment(in partialKey, out var segmentRoot))
            {
                segment = default;
                return false;
            }

            segment = new TriesSegment<TValue>(segmentRoot);
            return true;
        }

        public bool CopyFirstLayerKeys(string partialKey, List<string> output)
        {
            return CopyFirstLayerKeys(partialKey.AsMemory(), output);
        }

        public bool CopyFirstLayerKeys(ReadOnlyMemory<char> partialKey, List<string> output)
        {
            if (!root.TryGetSegment(partialKey, out var segRoot))
            {
                output.Clear();
                return false;
            }

            return segRoot.CopyFirstLayerKeys(output);
        }

        public bool CopyFirstLayerKeys<TKey>(in TKey partialKey, List<string> output) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (!root.TryGetSegment(in partialKey, out var segRoot))
            {
                output.Clear();
                return false;
            }

            return segRoot.CopyFirstLayerKeys(output);
        }

        #region IDictionary<string, TValue> explicit implementation

        bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

        ICollection<string> IDictionary<string, TValue>.Keys => Keys;

        ICollection<TValue> IDictionary<string, TValue>.Values => Values;

        int ICollection<KeyValuePair<string, TValue>>.Count => Count;

        TValue IDictionary<string, TValue>.this[string key]
        {
            get => this[key];
            set => this[key] = value;
        }

        void IDictionary<string, TValue>.Add(string key, TValue value) => Add(key, value);

        bool IDictionary<string, TValue>.ContainsKey(string key) => root.TryGetValue(key.AsMemory(), out _);

        bool IDictionary<string, TValue>.Remove(string key) => Remove(key);

        bool IDictionary<string, TValue>.TryGetValue(string key, out TValue value) => TryGetValue(key, out value);

        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => Add(item.Key, item.Value);

        void ICollection<KeyValuePair<string, TValue>>.Clear() => Clear();

        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            return root.TryGetValue(item.Key.AsMemory(), out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if ((uint)arrayIndex > (uint)array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Insufficient array length.");

            int i = arrayIndex;
            foreach (var kv in (IEnumerable<KeyValuePair<string, TValue>>)this)
            {
                array[i++] = kv;
            }
        }

        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            if (!((ICollection<KeyValuePair<string, TValue>>)this).Contains(item))
            {
                return false;
            }

            return Remove(item.Key);
        }

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
        {
            var keys = Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (root.TryGetValue(key.AsMemory(), out var value))
                {
                    yield return new KeyValuePair<string, TValue>(key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, TValue>>)this).GetEnumerator();

        #endregion
    }
}