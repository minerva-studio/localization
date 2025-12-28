using Minerva.Localizations.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using static Minerva.Localizations.L10nSymbols;

namespace Minerva.Localizations
{
    /// <summary>
    /// Localization key in construction
    /// </summary>
    public readonly struct Key : IEnumerable<string>, IEquatable<Key>, IReadOnlyList<string>
    {
        public static readonly Regex VALID_KEY_MEMBER = new(@"^[A-Za-z0-9_\-+]+$");
        public static readonly Regex VALID_KEY = new(@"(?:([A-Za-z0-9_-]+)\.?)+");

        public static readonly Key Empty = default;

        private const int MAX_INLINE_SEGMENTS = 4;

        private enum StorageMode : byte
        {
            Empty = 0,
            Inline = 1,
            Memory = 2,
        }

        private readonly StorageMode storageMode;
        private readonly int length;

        private readonly ReadOnlyMemory<char> inline0;
        private readonly ReadOnlyMemory<char> inline1;
        private readonly ReadOnlyMemory<char> inline2;
        private readonly ReadOnlyMemory<char> inline3;

        private readonly ReadOnlyMemory<char>[] segmentMemories;

        public int Length => length;

        public bool IsEmpty => length == 0;

        int IReadOnlyCollection<string>.Count => length;

        public string this[int index]
        {
            get
            {
                var span = GetSegmentSpan(index);
                return new string(span);
            }
        }

        public ArraySegment<string> Section
        {
            get
            {
                var arr = new string[length];
                for (int i = 0; i < length; i++)
                {
                    arr[i] = new string(GetSegmentSpan(i));
                }
                return new ArraySegment<string>(arr, 0, length);
            }
        }

        public Key this[Range range]
        {
            get
            {
                var (offset, len) = range.GetOffsetAndLength(length);
                if ((uint)offset > (uint)length || (uint)len > (uint)(length - offset))
                {
                    throw new IndexOutOfRangeException();
                }

                if (len == 0)
                {
                    return Empty;
                }

                if (offset == 0 && len == length)
                {
                    return this;
                }

                if (len <= MAX_INLINE_SEGMENTS)
                {
                    ReadOnlyMemory<char> m0 = default, m1 = default, m2 = default, m3 = default;
                    for (int i = 0; i < len; i++)
                    {
                        var mem = GetSegmentMemory(offset + i);
                        switch (i)
                        {
                            case 0: m0 = mem; break;
                            case 1: m1 = mem; break;
                            case 2: m2 = mem; break;
                            case 3: m3 = mem; break;
                        }
                    }
                    return new Key(m0, m1, m2, m3, len);
                }
                else
                {
                    var segs = new ReadOnlyMemory<char>[len];
                    for (int i = 0; i < len; i++)
                    {
                        segs[i] = GetSegmentMemory(offset + i);
                    }
                    return new Key(segs, len);
                }
            }
        }

        #region Constructor

        public Key(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                this = Empty;
                return;
            }

            int segCount = 1;
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] == KEY_SEPARATOR) segCount++;
            }

            length = segCount;
            segmentMemories = null;
            inline0 = inline1 = inline2 = inline3 = default;

            if (segCount <= MAX_INLINE_SEGMENTS)
            {
                storageMode = StorageMode.Inline;

                int start = 0;
                int segIndex = 0;
                for (int i = 0; i <= key.Length; i++)
                {
                    if (i == key.Length || key[i] == KEY_SEPARATOR)
                    {
                        var mem = key.AsMemory(start, i - start);
                        switch (segIndex)
                        {
                            case 0: inline0 = mem; break;
                            case 1: inline1 = mem; break;
                            case 2: inline2 = mem; break;
                            case 3: inline3 = mem; break;
                        }
                        segIndex++;
                        start = i + 1;
                    }
                }

                if (!ValidateSegmentsInline(this))
                    throw new ArgumentException($"'{key}' is not a valid member of a key");
            }
            else
            {
                storageMode = StorageMode.Memory;
                var segs = new ReadOnlyMemory<char>[segCount];

                int start = 0;
                int segIndex = 0;
                for (int i = 0; i <= key.Length; i++)
                {
                    if (i == key.Length || key[i] == KEY_SEPARATOR)
                    {
                        segs[segIndex++] = key.AsMemory(start, i - start);
                        start = i + 1;
                    }
                }

                segmentMemories = segs;

                if (!ValidateSegmentsMemory(this))
                    throw new ArgumentException($"'{key}' is not a valid member of a key");
            }
        }

        public Key(params string[] path)
        {
            if (path == null || path.Length == 0)
            {
                this = Empty;
                return;
            }

            length = path.Length;
            segmentMemories = null;
            inline0 = inline1 = inline2 = inline3 = default;

            if (length <= MAX_INLINE_SEGMENTS)
            {
                storageMode = StorageMode.Inline;
                for (int i = 0; i < length; i++)
                {
                    var mem = (path[i] ?? string.Empty).AsMemory();
                    switch (i)
                    {
                        case 0: inline0 = mem; break;
                        case 1: inline1 = mem; break;
                        case 2: inline2 = mem; break;
                        case 3: inline3 = mem; break;
                    }
                }

                if (!ValidateSegmentsInline(this))
                    throw new ArgumentException(string.Join(KEY_SEPARATOR, path));
            }
            else
            {
                storageMode = StorageMode.Memory;
                var segs = new ReadOnlyMemory<char>[length];
                for (int i = 0; i < length; i++)
                {
                    segs[i] = (path[i] ?? string.Empty).AsMemory();
                }
                segmentMemories = segs;

                if (!ValidateSegmentsMemory(this))
                    throw new ArgumentException(string.Join(KEY_SEPARATOR, path));
            }
        }

        public Key(Key baseKey, params string[] path)
        {
            this = Join(baseKey, path);
        }

        public Key(ReadOnlyMemory<char> keyMemory)
        {
            if (keyMemory.IsEmpty)
            {
                this = Empty;
                return;
            }

            var span = keyMemory.Span;

            int segCount = 1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == KEY_SEPARATOR) segCount++;
            }

            length = segCount;
            segmentMemories = null;
            inline0 = inline1 = inline2 = inline3 = default;

            if (segCount <= MAX_INLINE_SEGMENTS)
            {
                storageMode = StorageMode.Inline;

                int start = 0;
                int segIndex = 0;
                for (int i = 0; i <= span.Length; i++)
                {
                    if (i == span.Length || span[i] == KEY_SEPARATOR)
                    {
                        var mem = keyMemory.Slice(start, i - start);
                        switch (segIndex)
                        {
                            case 0: inline0 = mem; break;
                            case 1: inline1 = mem; break;
                            case 2: inline2 = mem; break;
                            case 3: inline3 = mem; break;
                        }
                        segIndex++;
                        start = i + 1;
                    }
                }

                if (!ValidateSegmentsInline(this))
                    throw new ArgumentException("Invalid key");
            }
            else
            {
                storageMode = StorageMode.Memory;
                var segs = new ReadOnlyMemory<char>[segCount];

                int start = 0;
                int segIndex = 0;
                for (int i = 0; i <= span.Length; i++)
                {
                    if (i == span.Length || span[i] == KEY_SEPARATOR)
                    {
                        segs[segIndex++] = keyMemory.Slice(start, i - start);
                        start = i + 1;
                    }
                }

                segmentMemories = segs;

                if (!ValidateSegmentsMemory(this))
                    throw new ArgumentException("Invalid key");
            }
        }

        public Key(ReadOnlySpan<char> keySpan)
        {
            if (keySpan.IsEmpty)
            {
                this = Empty;
                return;
            }

            this = new Key(new string(keySpan));
        }

        private Key(ReadOnlyMemory<char> m0, ReadOnlyMemory<char> m1, ReadOnlyMemory<char> m2, ReadOnlyMemory<char> m3, int len)
        {
            storageMode = len == 0 ? StorageMode.Empty : StorageMode.Inline;
            length = len;
            inline0 = m0;
            inline1 = m1;
            inline2 = m2;
            inline3 = m3;
            segmentMemories = null;
        }

        private Key(ReadOnlyMemory<char>[] segs, int len)
        {
            storageMode = len == 0 ? StorageMode.Empty : StorageMode.Memory;
            length = len;
            inline0 = inline1 = inline2 = inline3 = default;
            segmentMemories = segs;
        }

        #endregion

        public Key Append(string v)
        {
            v ??= string.Empty;

            int newLen = length + 1;

            if (newLen <= MAX_INLINE_SEGMENTS)
            {
                ReadOnlyMemory<char> m0 = inline0;
                ReadOnlyMemory<char> m1 = inline1;
                ReadOnlyMemory<char> m2 = inline2;
                ReadOnlyMemory<char> m3 = inline3;

                var mem = v.AsMemory();
                switch (length)
                {
                    case 0: m0 = mem; break;
                    case 1: m1 = mem; break;
                    case 2: m2 = mem; break;
                    case 3: m3 = mem; break;
                }

                return new Key(m0, m1, m2, m3, newLen);
            }

            var segs = new ReadOnlyMemory<char>[newLen];
            for (int i = 0; i < length; i++)
            {
                segs[i] = GetSegmentMemory(i);
            }
            segs[length] = v.AsMemory();

            return new Key(segs, newLen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<char> GetSegmentMemory(int index)
        {
            if ((uint)index >= (uint)length) throw new IndexOutOfRangeException();

            if (storageMode == StorageMode.Inline)
            {
                return index switch
                {
                    0 => inline0,
                    1 => inline1,
                    2 => inline2,
                    3 => inline3,
                    _ => default
                };
            }

            return segmentMemories[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetSegmentSpan(int index) => GetSegmentMemory(index).Span;

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < length; i++)
            {
                yield return new string(GetSegmentSpan(i));
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();






        public override string ToString() => KeyStringCache.Shared.GetString(in this);

        public bool Equals(Key other)
        {
            if (length != other.length) return false;
            if (length == 0) return true;

            for (int i = 0; i < length; i++)
            {
                if (!GetSegmentSpan(i).SequenceEqual(other.GetSegmentSpan(i)))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => obj is Key other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(length);

            for (int i = 0; i < length; i++)
            {
                var seg = GetSegmentSpan(i);
                for (int j = 0; j < seg.Length; j++)
                {
                    hash.Add(seg[j]);
                }
                hash.Add(KEY_SEPARATOR);
            }

            return hash.ToHashCode();
        }

        public static bool operator ==(Key left, Key right) => left.Equals(right);

        public static bool operator !=(Key left, Key right) => !left.Equals(right);

        public static Key operator +(Key a, Key b)
        {
            return Join_Internal(a, b);
        }

        public static Key operator +(Key key, string next) => key.Append(next);

        public static Key operator -(Key key, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");

            int newLen = key.length - count;
            if (newLen <= 0) return Empty;
            if (newLen == key.length) return key;

            if (newLen <= MAX_INLINE_SEGMENTS)
            {
                ReadOnlyMemory<char> m0 = default, m1 = default, m2 = default, m3 = default;
                for (int i = 0; i < newLen; i++)
                {
                    var mem = key.GetSegmentMemory(i);
                    switch (i)
                    {
                        case 0: m0 = mem; break;
                        case 1: m1 = mem; break;
                        case 2: m2 = mem; break;
                        case 3: m3 = mem; break;
                    }
                }
                return new Key(m0, m1, m2, m3, newLen);
            }
            else
            {
                var segs = new ReadOnlyMemory<char>[newLen];
                for (int i = 0; i < newLen; i++)
                {
                    segs[i] = key.GetSegmentMemory(i);
                }
                return new Key(segs, newLen);
            }
        }


        public static implicit operator string(Key key) => KeyStringCache.Shared.GetString(in key);

        public static implicit operator ArraySegment<string>(Key key) => key.Section;

        public static explicit operator Key(string key) => new Key(key);

        public static explicit operator Key(ReadOnlyMemory<char> keyMemory) => new Key(keyMemory);




        #region Helper

        private static bool ValidateSegmentsInline(Key key)
        {
            for (int i = 0; i < key.length; i++)
            {
                var seg = key.GetSegmentSpan(i);
                if (seg.IsEmpty) return false;

                foreach (var c in seg)
                {
                    if (!IsValidChar(c))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool ValidateSegmentsMemory(Key key) => ValidateSegmentsInline(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '+';
        }

        #endregion





        public static string JoinString(params string[] s)
        {
            return KeyStringCache.Shared.GetString(new Key(s));
        }

        public static string JoinString(string[] s, params string[] s2)
        {
            var combined = new string[(s?.Length ?? 0) + (s2?.Length ?? 0)];
            int idx = 0;
            if (s != null)
            {
                Array.Copy(s, 0, combined, idx, s.Length);
                idx += s.Length;
            }
            if (s2 != null)
            {
                Array.Copy(s2, 0, combined, idx, s2.Length);
            }
            return KeyStringCache.Shared.GetString(new Key(combined));
        }

        public static string JoinString(Key key, params string[] s2)
        {
            if (s2 == null || s2.Length == 0)
            {
                return KeyStringCache.Shared.GetString(in key);
            }
            return KeyStringCache.Shared.GetString(key + new Key(s2));
        }






        private static Key Join_Internal(Key a, Key b)
        {
            if (a.length == 0) return b;
            if (b.length == 0) return a;

            int newLen = a.length + b.length;

            if (newLen <= MAX_INLINE_SEGMENTS)
            {
                ReadOnlyMemory<char> m0 = default, m1 = default, m2 = default, m3 = default;
                for (int i = 0; i < a.length; i++)
                {
                    var mem = a.GetSegmentMemory(i);
                    switch (i)
                    {
                        case 0: m0 = mem; break;
                        case 1: m1 = mem; break;
                        case 2: m2 = mem; break;
                        case 3: m3 = mem; break;
                    }
                }

                for (int i = 0; i < b.length; i++)
                {
                    var mem = b.GetSegmentMemory(i);
                    int idx = a.length + i;
                    switch (idx)
                    {
                        case 0: m0 = mem; break;
                        case 1: m1 = mem; break;
                        case 2: m2 = mem; break;
                        case 3: m3 = mem; break;
                    }
                }

                return new Key(m0, m1, m2, m3, newLen);
            }

            var segs = new ReadOnlyMemory<char>[newLen];
            for (int i = 0; i < a.length; i++)
            {
                segs[i] = a.GetSegmentMemory(i);
            }
            for (int i = 0; i < b.length; i++)
            {
                segs[a.length + i] = b.GetSegmentMemory(i);
            }
            return new Key(segs, newLen);
        }

        public static Key Join(Key key1, Key key2) => Join_Internal(key1, key2);

        public static Key Join(Key key, params string[] s2) => Join_Internal(key, new Key(s2));

        public static Key Join(params string[] s)
        {
            var builder = KeyBuilder.Builder;
            builder.Clear();

            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    builder.Append(s[i]);
                }
            }

            var result = builder.Build();
            builder.Clear();
            return result;
        }

        public static Key Join(string s1, string s2)
        {
            var builder = KeyBuilder.Builder;
            builder.Clear();
            builder.Append(s1);
            builder.Append(s2);
            var result = builder.Build();
            builder.Clear();
            return result;
        }

        public static Key Join(string s1, params string[] s2)
        {
            var builder = KeyBuilder.Builder;
            builder.Clear();
            builder.Append(s1);

            if (s2 != null)
            {
                for (int i = 0; i < s2.Length; i++)
                {
                    builder.Append(s2[i]);
                }
            }

            var result = builder.Build();
            builder.Clear();
            return result;
        }

        public static Key Join(string s1, IReadOnlyList<string> s2)
        {
            var builder = KeyBuilder.Builder;
            builder.Clear();
            builder.Append(s1);

            if (s2 != null)
            {
                for (int i = 0; i < s2.Count; i++)
                {
                    builder.Append(s2[i]);
                }
            }

            var result = builder.Build();
            builder.Clear();
            return result;
        }




        #region KeyBuilder (per-thread, reusable)

        public sealed class KeyBuilder
        {
            private const int KEY_BUILDER_DEFAULT_CAPACITY = 8;
            private static readonly ThreadLocal<KeyBuilder> keyBuilder = new ThreadLocal<KeyBuilder>(static () => new KeyBuilder(KEY_BUILDER_DEFAULT_CAPACITY));

            public static KeyBuilder Builder => keyBuilder.Value;


            private ReadOnlyMemory<char>[] segments;
            private int count;

            public int Count => count;

            public KeyBuilder(int capacity)
            {
                segments = new ReadOnlyMemory<char>[capacity <= 0 ? KEY_BUILDER_DEFAULT_CAPACITY : capacity];
                count = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear()
            {
                count = 0;
            }

            public void Append(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                int start = 0;
                for (int i = 0; i <= value.Length; i++)
                {
                    if (i == value.Length || value[i] == KEY_SEPARATOR)
                    {
                        int len = i - start;
                        if (len > 0)
                        {
                            EnsureCapacity(count + 1);
                            segments[count++] = value.AsMemory(start, len);
                        }
                        start = i + 1;
                    }
                }
            }

            public void Append(Key key)
            {
                for (int i = 0; i < key.length; i++)
                {
                    EnsureCapacity(count + 1);
                    segments[count++] = key.GetSegmentMemory(i);
                }
            }

            public Key Build(bool validate = true)
            {
                if (count == 0)
                {
                    return Empty;
                }

                if (count <= MAX_INLINE_SEGMENTS)
                {
                    ReadOnlyMemory<char> m0 = default;
                    ReadOnlyMemory<char> m1 = default;
                    ReadOnlyMemory<char> m2 = default;
                    ReadOnlyMemory<char> m3 = default;

                    switch (count)
                    {
                        case 4: m3 = segments[3]; goto case 3;
                        case 3: m2 = segments[2]; goto case 2;
                        case 2: m1 = segments[1]; goto case 1;
                        case 1: m0 = segments[0]; break;
                    }

                    var key = new Key(m0, m1, m2, m3, count);
                    if (validate && !ValidateSegmentsInline(key))
                    {
                        throw new ArgumentException("Invalid key");
                    }
                    Clear();
                    return key;
                }
                else
                {
                    var segs = new ReadOnlyMemory<char>[count];
                    Array.Copy(segments, 0, segs, 0, count);

                    var key = new Key(segs, count);
                    if (validate && !ValidateSegmentsMemory(key))
                    {
                        throw new ArgumentException("Invalid key");
                    }
                    Clear();
                    return key;
                }
            }

            private void EnsureCapacity(int needed)
            {
                if (segments.Length >= needed)
                {
                    return;
                }

                int newSize = segments.Length == 0 ? KEY_BUILDER_DEFAULT_CAPACITY : segments.Length * 2;
                if (newSize < needed)
                {
                    newSize = needed;
                }

                Array.Resize(ref segments, newSize);
            }
        }

        #endregion
    }
}