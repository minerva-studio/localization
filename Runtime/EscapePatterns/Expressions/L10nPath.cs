using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Minerva.Localizations.EscapePatterns
{
    internal sealed class L10nPath
    {
        internal const int MaximumInternedKeyNodesPerPath = 256;

        public enum SegmentKind : byte { Member, LiteralIndex, DynamicIndex }

        internal readonly struct Segment
        {
            public readonly SegmentKind Kind;
            public readonly string Name;
            public readonly int Index;
            public readonly L10nExpression Expression;

            private Segment(SegmentKind kind, string name, int index, L10nExpression expression)
            {
                Kind = kind;
                Name = name;
                Index = index;
                Expression = expression;
            }

            public static Segment Member(string name) => new(SegmentKind.Member, name, 0, null);
            public static Segment LiteralIndex(int index) => new(SegmentKind.LiteralIndex, null, index, null);
            public static Segment DynamicIndex(L10nExpression expression) => new(SegmentKind.DynamicIndex, null, 0, expression);
        }

        private sealed class KeyNode
        {
            public readonly string Key;
            public KeyNode Next;
            public readonly ConcurrentDictionary<int, KeyNode> ByIndex;

            public KeyNode(string key, bool dynamic = false)
            {
                Key = key;
                if (dynamic) ByIndex = new ConcurrentDictionary<int, KeyNode>();
            }
        }

        private readonly KeyNode dynamicRoot;
        private readonly Segment[] segments;
        private readonly string[] prefixKeys;
        private readonly bool hasDynamicIndex;
        private readonly object internLock = new();
        private int internedKeyNodeCount;

        public int SegmentCount => segments.Length;
        internal int InternedKeyNodeCount => Volatile.Read(ref internedKeyNodeCount);
        public L10nParams? Args { get; }
        public Segment GetSegment(int index) => segments[index];

        public L10nPath(Segment[] segments, L10nParams? args)
        {
            this.segments = segments;
            Args = args;
            hasDynamicIndex = Array.Exists(segments, static s => s.Kind == SegmentKind.DynamicIndex);
            if (hasDynamicIndex) dynamicRoot = new KeyNode(null);
            if (!hasDynamicIndex)
            {
                prefixKeys = new string[segments.Length];
                string key = string.Empty;
                for (int i = 0; i < segments.Length; i++)
                {
                    key = AppendSegment(key, segments[i], 0);
                    prefixKeys[i] = key;
                }
            }
        }

        public void BuildPrefixKeys(int[] indices, int indexBase, string[] keys)
        {
            if (!hasDynamicIndex)
            {
                Array.Copy(prefixKeys, keys, segments.Length);
                return;
            }
            KeyNode parent = dynamicRoot;
            string currentKey = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                Segment segment = segments[i];
                if (segment.Kind == SegmentKind.DynamicIndex && parent != null)
                {
                    int index = indices[indexBase + i];
                    var branches = parent.ByIndex;
                    if (!branches.TryGetValue(index, out var child))
                    {
                        lock (internLock)
                        {
                            if (!branches.TryGetValue(index, out child) && internedKeyNodeCount < MaximumInternedKeyNodesPerPath)
                            {
                                child = new KeyNode(AppendSegment(parent.Key, segment, index), i + 1 < segments.Length && segments[i + 1].Kind == SegmentKind.DynamicIndex);
                                branches.TryAdd(index, child);
                                internedKeyNodeCount++;
                            }
                        }
                    }

                    if (child == null)
                    {
                        currentKey = AppendSegment(currentKey, segment, index);
                        parent = null;
                    }
                    else
                    {
                        parent = child;
                        currentKey = child.Key;
                    }
                }
                else if (parent != null)
                {
                    var child = Volatile.Read(ref parent.Next);
                    if (child == null)
                    {
                        lock (internLock)
                        {
                            child = parent.Next;
                            if (child == null && internedKeyNodeCount < MaximumInternedKeyNodesPerPath)
                            {
                                child = new KeyNode(AppendSegment(parent.Key, segment, 0), i + 1 < segments.Length && segments[i + 1].Kind == SegmentKind.DynamicIndex);
                                Volatile.Write(ref parent.Next, child);
                                internedKeyNodeCount++;
                            }
                        }
                    }

                    if (child == null)
                    {
                        currentKey = AppendSegment(currentKey, segment, 0);
                        parent = null;
                    }
                    else
                    {
                        parent = child;
                        currentKey = child.Key;
                    }
                }
                else
                {
                    int index = segment.Kind == SegmentKind.DynamicIndex ? indices[indexBase + i] : 0;
                    currentKey = AppendSegment(currentKey, segment, index);
                }
                keys[i] = currentKey;
            }
        }

        public bool TryWalk(L10nValue current, int fromSegment, ReadOnlySpan<int> indices, out L10nValue value)
        {
            if (fromSegment >= segments.Length)
            {
                value = current;
                return !value.IsNull;
            }
            for (int i = fromSegment; i < segments.Length; i++)
            {
                if (current.IsNull) { value = default; return false; }
                Segment segment = segments[i];
                if (segment.Kind == SegmentKind.Member)
                {
                    if (current.Kind == L10nValue.ValueKind.Number || !Minerva.Localizations.Utilities.Reflection.TryGetMember(current.ToObject(), segment.Name, out current)) { value = default; return false; }
                }
                else
                {
                    int index = segment.Kind == SegmentKind.LiteralIndex ? segment.Index : indices[i];
                    if (!TryGetIndex(current, index, out current)) { value = default; return false; }
                }
            }
            value = current;
            return !value.IsNull;
        }

        private static bool TryGetIndex(L10nValue current, int index, out L10nValue value)
        {
            object target = current.ToObject();
            switch (target)
            {
                case IList<int> list when index >= 0 && index < list.Count: value = L10nValue.FromNumber(list[index]); return true;
                case IList<float> list when index >= 0 && index < list.Count: value = L10nValue.FromNumber(list[index]); return true;
                case IList<double> list when index >= 0 && index < list.Count: value = L10nValue.FromNumber(list[index]); return true;
                case IList list when index >= 0 && index < list.Count: value = L10nValue.FromObject(list[index]); return !value.IsNull;
                default: value = default; return false;
            }
        }

        public static L10nPath ParseCanonical(string key)
        {
            var span = key.AsSpan();
            int position = 0;
            var segments = new List<Segment>();
            if (!TryReadIdentifier(span, ref position, out var member)) throw new FormatException("Expected member at position 0.");
            segments.Add(Segment.Member(member));
            while (position < span.Length)
            {
                if (span[position] == '.')
                {
                    position++;
                    if (!TryReadIdentifier(span, ref position, out member)) throw new FormatException($"Expected member at position {position}.");
                    segments.Add(Segment.Member(member));
                }
                else if (span[position] == '[')
                {
                    int start = ++position;
                    bool negative = position < span.Length && span[position] == '-';
                    if (negative) position++;
                    int digitsStart = position;
                    while (position < span.Length && char.IsDigit(span[position])) position++;
                    if (digitsStart == position || position >= span.Length || span[position] != ']' ||
                        !int.TryParse(span.Slice(start, position - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                        throw new FormatException($"Expected literal Int32 index at position {start}.");
                    position++;
                    segments.Add(Segment.LiteralIndex(index));
                }
                else throw new FormatException($"Unexpected character at position {position}.");
            }
            return new L10nPath(segments.ToArray(), null);
        }

        private static bool TryReadIdentifier(ReadOnlySpan<char> source, ref int position, out string value)
        {
            int start = position;
            if (position >= source.Length || !(char.IsLetter(source[position]) || source[position] == '_')) { value = null; return false; }
            position++;
            while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] == '_')) position++;
            value = source.Slice(start, position - start).ToString();
            return true;
        }

        private static string AppendSegment(string prefix, Segment segment, int dynamicIndex)
        {
            return segment.Kind switch
            {
                SegmentKind.Member => string.IsNullOrEmpty(prefix) ? segment.Name : string.Concat(prefix, ".", segment.Name),
                SegmentKind.LiteralIndex => string.Concat(prefix, "[", segment.Index.ToString(CultureInfo.InvariantCulture), "]"),
                _ => string.Concat(prefix, "[", dynamicIndex.ToString(CultureInfo.InvariantCulture), "]")
            };
        }
    }
}
