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
    public readonly struct Key : IEnumerable<string>, IEquatable<Key>, IReadOnlyList<string>, IReadOnlyList<ReadOnlyMemory<char>>
    {
        private readonly struct InlineSegments
        {
            public const int MAX_INLINE_SEGMENTS = 8;

            private readonly ReadOnlyMemory<char> s0;
            private readonly ReadOnlyMemory<char> s1;
            private readonly ReadOnlyMemory<char> s2;
            private readonly ReadOnlyMemory<char> s3;
            private readonly ReadOnlyMemory<char> s4;
            private readonly ReadOnlyMemory<char> s5;
            private readonly ReadOnlyMemory<char> s6;
            private readonly ReadOnlyMemory<char> s7;

            public InlineSegments(
                ReadOnlyMemory<char> s0,
                ReadOnlyMemory<char> s1,
                ReadOnlyMemory<char> s2,
                ReadOnlyMemory<char> s3,
                ReadOnlyMemory<char> s4,
                ReadOnlyMemory<char> s5,
                ReadOnlyMemory<char> s6,
                ReadOnlyMemory<char> s7)
            {
                this.s0 = s0;
                this.s1 = s1;
                this.s2 = s2;
                this.s3 = s3;
                this.s4 = s4;
                this.s5 = s5;
                this.s6 = s6;
                this.s7 = s7;
            }

            public ReadOnlyMemory<char> this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Get(index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlyMemory<char> Get(int index)
            {
                return index switch
                {
                    0 => s0,
                    1 => s1,
                    2 => s2,
                    3 => s3,
                    4 => s4,
                    5 => s5,
                    6 => s6,
                    7 => s7,
                    _ => default
                };
            }
        }

        public static readonly Regex VALID_KEY_MEMBER = new(@"^[A-Za-z0-9_\-+]+$");
        public static readonly Regex VALID_KEY = new(@"(?:([A-Za-z0-9_-]+)\.?)+");

        public static readonly Key Empty = default;

        private enum StorageMode : byte
        {
            Empty = 0,
            Inline = 1,
            Memory = 2,
        }

        private readonly StorageMode storageMode;
        private readonly int length;

        private readonly InlineSegments inlineSegments;
        private readonly ReadOnlyMemory<char>[] segmentMemories;

        public int Length => length;

        public bool IsEmpty => length == 0;

        int IReadOnlyCollection<string>.Count => length;
        int IReadOnlyCollection<ReadOnlyMemory<char>>.Count => length;

        public string this[int index]
        {
            get
            {
                var span = GetSegmentSpan(index);
                return new string(span);
            }
        }
        ReadOnlyMemory<char> IReadOnlyList<ReadOnlyMemory<char>>.this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetSegmentMemory(index);
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

                if (len <= InlineSegments.MAX_INLINE_SEGMENTS)
                {
                    ReadOnlyMemory<char> m0 = default, m1 = default, m2 = default, m3 = default, m4 = default, m5 = default, m6 = default, m7 = default;
                    for (int i = 0; i < len; i++)
                    {
                        var mem = GetSegmentMemory(offset + i);
                        switch (i)
                        {
                            case 0: m0 = mem; break;
                            case 1: m1 = mem; break;
                            case 2: m2 = mem; break;
                            case 3: m3 = mem; break;
                            case 4: m4 = mem; break;
                            case 5: m5 = mem; break;
                            case 6: m6 = mem; break;
                            case 7: m7 = mem; break;
                        }
                    }
                    return new Key(m0, m1, m2, m3, m4, m5, m6, m7, len);
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

            if (segCount <= InlineSegments.MAX_INLINE_SEGMENTS)
            {
                storageMode = StorageMode.Inline;

                ReadOnlyMemory<char> m0 = default, m1 = default, m2 = default, m3 = default, m4 = default, m5 = default, m6 = default, m7 = default;

                int start = 0;
                int segIndex = 0;
                for (int i = 0; i <= key.Length; i++)
                {
                    if (i == key.Length || key[i] == KEY_SEPARATOR)
                    {
                        var mem = key.AsMemory(start, i - start);
                        switch (segIndex)
                        {
                            case 0: m0 = mem; break;
                            case 1: m1 = mem; break;
                            case 2: m2 = mem; break;
                            case 3: m3 = mem; break;
                            case 4: m4 = mem; break;
                            case 5: m5 = mem; break;
                            case 6: m6 = mem; break;
                            case 7: m7 = mem; break;
                        }
                        segIndex++;
                        start = i + 1;
                    }
                }

                inlineSegments = new InlineSegments(m0, m1, m2, m3, m4, m5, m6, m7);

                if (!ValidateSegmentsInline(this))
                    throw new ArgumentException($"'{key}' is not a valid member of a key");
            }
            else
            {
                storageMode = StorageMode.Memory;
                inlineSegments = default;

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
            this = new Key((IReadOnlyList<string>)path);
        }

        public Key(IReadOnlyList<string> path)
        {
            this = Join(path);
        }

        private Key(
            ReadOnlyMemory<char> m0,
            ReadOnlyMemory<char> m1,
            ReadOnlyMemory<char> m2,
            ReadOnlyMemory<char> m3,
            ReadOnlyMemory<char> m4,
            ReadOnlyMemory<char> m5,
            ReadOnlyMemory<char> m6,
            ReadOnlyMemory<char> m7,
            int len)
        {
            storageMode = len == 0 ? StorageMode.Empty : StorageMode.Inline;
            length = len;
            inlineSegments = new InlineSegments(m0, m1, m2, m3, m4, m5, m6, m7);
            segmentMemories = null;
        }

        private Key(ReadOnlyMemory<char>[] segs, int len)
        {
            storageMode = len == 0 ? StorageMode.Empty : StorageMode.Memory;
            length = len;
            inlineSegments = default;
            segmentMemories = segs;
        }

        #endregion 

        public Key Append(string v)
        {
            v ??= string.Empty;

            int newLen = length + 1;

            if (newLen <= InlineSegments.MAX_INLINE_SEGMENTS)
            {
                ReadOnlyMemory<char> m0 = inlineSegments.Get(0);
                ReadOnlyMemory<char> m1 = inlineSegments.Get(1);
                ReadOnlyMemory<char> m2 = inlineSegments.Get(2);
                ReadOnlyMemory<char> m3 = inlineSegments.Get(3);
                ReadOnlyMemory<char> m4 = inlineSegments.Get(4);
                ReadOnlyMemory<char> m5 = inlineSegments.Get(5);
                ReadOnlyMemory<char> m6 = inlineSegments.Get(6);
                ReadOnlyMemory<char> m7 = inlineSegments.Get(7);

                var mem = v.AsMemory();
                switch (length)
                {
                    case 0: m0 = mem; break;
                    case 1: m1 = mem; break;
                    case 2: m2 = mem; break;
                    case 3: m3 = mem; break;
                    case 4: m4 = mem; break;
                    case 5: m5 = mem; break;
                    case 6: m6 = mem; break;
                    case 7: m7 = mem; break;
                }

                return new Key(m0, m1, m2, m3, m4, m5, m6, m7, newLen);
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
                return inlineSegments.Get(index);
            }

            return segmentMemories[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetSegmentSpan(int index) => GetSegmentMemory(index).Span;

        public IEnumerator<ReadOnlyMemory<char>> GetEnumerator()
        {
            for (int i = 0; i < length; i++)
            {
                yield return GetSegmentMemory(i);
            }
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
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

        public static bool operator ==(in Key left, in Key right) => left.Equals(right);

        public static bool operator !=(in Key left, in Key right) => !left.Equals(right);

        public static Key operator +(in Key a, in Key b) => Join(in a, in b);

        public static Key operator +(in Key key, string next) => Key.Join(key, next, Array.Empty<string>());

        public static Key operator +(in Key key, IReadOnlyList<string> str) => Join(in key, str);

        public static Key operator -(in Key key, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");

            int newLen = key.length - count;
            if (newLen <= 0) return Empty;
            if (newLen == key.length) return key;

            if (newLen <= InlineSegments.MAX_INLINE_SEGMENTS)
            {
                ReadOnlyMemory<char> m0 = default, m1 = default, m2 = default, m3 = default, m4 = default, m5 = default, m6 = default, m7 = default;
                for (int i = 0; i < newLen; i++)
                {
                    var mem = key.GetSegmentMemory(i);
                    switch (i)
                    {
                        case 0: m0 = mem; break;
                        case 1: m1 = mem; break;
                        case 2: m2 = mem; break;
                        case 3: m3 = mem; break;
                        case 4: m4 = mem; break;
                        case 5: m5 = mem; break;
                        case 6: m6 = mem; break;
                        case 7: m7 = mem; break;
                    }
                }
                return new Key(m0, m1, m2, m3, m4, m5, m6, m7, newLen);
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

        public static implicit operator string(in Key key) => KeyStringCache.Shared.GetString(in key);

        public static explicit operator Key(string key) => new Key(key);

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
            return KeyStringCache.Shared.GetString(Join(s));
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

        public static string JoinString(in Key key, params string[] s2)
        {
            if (s2 == null || s2.Length == 0)
            {
                return KeyStringCache.Shared.GetString(in key);
            }
            return KeyStringCache.Shared.GetString(key + new Key(s2));
        }




        public static Key Join(in Key a, in Key b)
        {
            if (b.length == 0) return a;
            if (a.length == 0) return b;

            using var builder = KeyBuilder.Builder;
            builder.Append(a);
            builder.Append(b);
            var result = builder.Build();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Key Join(in Key key, params string[] s2) => Join(in key, (IReadOnlyList<string>)s2);

        public static Key Join(in Key key, IReadOnlyList<string> s)
        {
            if (s.Count == 0) return key;

            using var builder = KeyBuilder.Builder;
            builder.Append(key);
            foreach (var seg in s)
            {
                builder.Append(seg);
            }
            var result = builder.Build();
            return result;
        }

        public static Key Join(in Key key, string s1, IReadOnlyList<string> strings)
        {
            if (strings.Count == 0 && !s1.Contains(KEY_SEPARATOR))
                return key.Append(s1);

            using var builder = KeyBuilder.Builder;
            builder.Append(key);
            builder.Append(s1);
            if (strings != null)
            {
                for (int i = 0; i < strings.Count; i++)
                {
                    builder.Append(strings[i]);
                }
            }
            var result = builder.Build();
            return result;
        }

        public static Key Join(params string[] s) => Join((IReadOnlyList<string>)s);

        public static Key Join(IReadOnlyList<string> s)
        {
            using var builder = KeyBuilder.Builder;

            if (s != null)
            {
                for (int i = 0; i < s.Count; i++)
                {
                    builder.Append(s[i]);
                }
            }

            var result = builder.Build();
            return result;
        }

        public static Key Join(string s1, string s2)
        {
            using var builder = KeyBuilder.Builder;
            builder.Append(s1);
            builder.Append(s2);
            var result = builder.Build();
            return result;
        }

        public static Key Join(string s1, params string[] s2)
        {
            using var builder = KeyBuilder.Builder;
            builder.Append(s1);

            if (s2 != null)
            {
                for (int i = 0; i < s2.Length; i++)
                {
                    builder.Append(s2[i]);
                }
            }

            var result = builder.Build();
            return result;
        }

        public static Key Join(string s1, IReadOnlyList<string> s2)
        {
            using var builder = KeyBuilder.Builder;
            builder.Append(s1);

            if (s2 != null)
            {
                for (int i = 0; i < s2.Count; i++)
                {
                    builder.Append(s2[i]);
                }
            }

            var result = builder.Build();
            return result;
        }

        public string ToEscape()
        {
            return $"${ToString()}$";
        }

        public string ToTooltipEscape()
        {
            return $"$@{ToString()}$";
        }

        #region KeyBuilder (per-thread, reusable)

        public sealed class KeyBuilder : IDisposable
        {
            private const int KEY_BUILDER_DEFAULT_CAPACITY = 8;
            private static readonly ThreadLocal<KeyBuilder> keyBuilder = new ThreadLocal<KeyBuilder>(static () => new KeyBuilder(KEY_BUILDER_DEFAULT_CAPACITY));

            public static KeyBuilder Builder
            {
                get
                {
                    KeyBuilder value = keyBuilder.Value;
                    value.Clear();
                    return value;
                }
            }

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

                if (count <= InlineSegments.MAX_INLINE_SEGMENTS)
                {
                    ReadOnlyMemory<char> m0 = default;
                    ReadOnlyMemory<char> m1 = default;
                    ReadOnlyMemory<char> m2 = default;
                    ReadOnlyMemory<char> m3 = default;
                    ReadOnlyMemory<char> m4 = default;
                    ReadOnlyMemory<char> m5 = default;
                    ReadOnlyMemory<char> m6 = default;
                    ReadOnlyMemory<char> m7 = default;

                    switch (count)
                    {
                        case 8: m7 = segments[7]; goto case 7;
                        case 7: m6 = segments[6]; goto case 6;
                        case 6: m5 = segments[5]; goto case 5;
                        case 5: m4 = segments[4]; goto case 4;
                        case 4: m3 = segments[3]; goto case 3;
                        case 3: m2 = segments[2]; goto case 2;
                        case 2: m1 = segments[1]; goto case 1;
                        case 1: m0 = segments[0]; break;
                    }

                    var key = new Key(m0, m1, m2, m3, m4, m5, m6, m7, count);
                    if (validate && !ValidateSegmentsInline(key))
                    {
                        throw new ArgumentException("Invalid key");
                    }
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

            public void Dispose()
            {
                Clear();
            }
        }

        #endregion
    }
}