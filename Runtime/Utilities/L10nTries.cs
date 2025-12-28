using Minerva.Localizations.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Minerva.Localizations
{
    internal readonly struct TriesSegment<TValue>
    {
        private readonly L10nTriesNode<TValue> root;

        public bool HasValue => root != null;
        public int Count => root?.Count ?? 0;

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

            return root.TryGetValue(key, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(ReadOnlySpan<char> key, out TValue value)
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
        public bool ContainsKey(ReadOnlySpan<char> key) => TryGetValue(key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey<TKey>(in TKey key) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>> => TryGetValue(in key, out _);

        public bool TryGetSegment(ReadOnlySpan<char> partialKey, out TriesSegment<TValue> segment)
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

        public bool CopyFirstLayerKeys(ReadOnlySpan<char> partialKey, List<string> output)
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

        public void Clear()
        {
            Children.Clear();
            isTerminated = false;
            value = default;
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

            var node = this;

            int segmentCount = 1;
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] == '.') segmentCount++;
            }

            Span<L10nTriesNode<TValue>> path = new L10nTriesNode<TValue>[segmentCount + 1];

            int pathLen = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetValue(string key, out TValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return TryGetSelfValue(out value);
            }

            return TryGetValue(key.AsSpan(), out value);
        }

        internal bool TryGetValue(ReadOnlySpan<char> key, out TValue value)
        {
            if (key.IsEmpty)
            {
                return TryGetSelfValue(out value);
            }

            if (!TryGetNode(key, out var node))
            {
                value = default;
                return false;
            }

            return node.TryGetSelfValue(out value);
        }

        internal bool TryGetValue<TKey>(in TKey key, out TValue value) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (key.Count == 0)
            {
                return TryGetSelfValue(out value);
            }

            if (!TryGetNode(in key, out var node))
            {
                value = default;
                return false;
            }

            return node.TryGetSelfValue(out value);
        }

        internal bool TryGetSegment(ReadOnlySpan<char> partialKey, out L10nTriesNode<TValue> segmentRoot)
        {
            if (partialKey.IsEmpty)
            {
                segmentRoot = this;
                return true;
            }

            return TryGetNode(partialKey, out segmentRoot);
        }

        internal bool TryGetSegment<TKey>(in TKey partialKey, out L10nTriesNode<TValue> segmentRoot) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            if (partialKey.Count == 0)
            {
                segmentRoot = this;
                return true;
            }

            return TryGetNode(in partialKey, out segmentRoot);
        }

        internal bool CopyFirstLayerKeys(List<string> output)
        {
            output.Clear();

            if (Children == null || Children.Count == 0)
            {
                return true;
            }

            foreach (var kv in Children)
            {
                output.Add(kv.Key.ToString());
            }

            return true;
        }

        private bool TryGetNode(ReadOnlySpan<char> key, out L10nTriesNode<TValue> node)
        {
            node = this;
            var cursor = new L10nKeyCursor(key);

            while (cursor.TryNext(out var seg))
            {
                if (!node.TryGetChild(seg, out node))
                {
                    node = null;
                    return false;
                }
            }

            return true;
        }

        private bool TryGetNode<TKey>(in TKey key, out L10nTriesNode<TValue> node) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>>
        {
            node = this;

            for (int i = 0; i < key.Count; i++)
            {
                var seg = key[i].Span;
                if (!node.TryGetChild(seg, out node))
                {
                    node = null;
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetChild(ReadOnlySpan<char> seg, out L10nTriesNode<TValue> child)
        {
            if (Children.Count == 0)
            {
                child = null;
                return false;
            }

            foreach (var kv in Children)
            {
                if (kv.Key.Span.SequenceEqual(seg))
                {
                    child = kv.Value;
                    return true;
                }
            }

            child = null;
            return false;
        }
    }

    internal sealed class L10nTries<TValue>
    {
        private readonly L10nTriesNode<TValue> root = new L10nTriesNode<TValue>();

        public int Count => root.Count;

        public TriesSegment<TValue> RootSegment => new TriesSegment<TValue>(root);

        public TValue this[string key]
        {
            get
            {
                if (!root.TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException($"Key '{key}' not found in L10nTries.");
                }
                return value;
            }
            set => Set(key, value);
        }

        public void Clear() => root.Clear();

        public void Set(string key, TValue value) => root.Set(key, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out TValue value) => root.TryGetValue(key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(ReadOnlySpan<char> key, out TValue value) => root.TryGetValue(key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<TKey>(in TKey key, out TValue value) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>> => root.TryGetValue(in key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(ReadOnlySpan<char> key) => root.TryGetValue(key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey<TKey>(in TKey key) where TKey : struct, IReadOnlyList<ReadOnlyMemory<char>> => root.TryGetValue(in key, out _);

        public bool TryGetSegment(ReadOnlySpan<char> partialKey, out TriesSegment<TValue> segment)
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

        public bool CopyFirstLayerKeys(ReadOnlySpan<char> partialKey, List<string> output)
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
    }
}