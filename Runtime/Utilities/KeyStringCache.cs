using System;
using System.Runtime.CompilerServices;
using System.Text;
using static Minerva.Localizations.L10nSymbols;

namespace Minerva.Localizations.Utilities
{
    public sealed class KeyStringCache
    {
        internal const int MaximumEntries = 16384;
        public static KeyStringCache Shared { get; } = new KeyStringCache();

        private readonly BoundedConcurrentCache<Key, string> cache;

        private KeyStringCache()
        {
            cache = new BoundedConcurrentCache<Key, string>(MaximumEntries);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetString(in Key key)
        {
            if (key.Length == 0)
            {
                return string.Empty;
            }

            if (cache.TryGetValue(key, out var value)) return value;
            return cache.GetOrAdd(key, BuildString(in key));
        }

        private static string BuildString(in Key key)
        {
            if (key.Length == 1)
            {
                return new string(key.GetSegmentSpan(0));
            }

            int totalLen = key.Length - 1;
            for (int i = 0; i < key.Length; i++)
            {
                totalLen += key.GetSegmentMemory(i).Length;
            }

            if (totalLen <= 256)
            {
                Span<char> buffer = stackalloc char[totalLen];
                int pos = 0;

                for (int i = 0; i < key.Length; i++)
                {
                    if (i > 0)
                    {
                        buffer[pos++] = KEY_SEPARATOR;
                    }

                    var seg = key.GetSegmentSpan(i);
                    seg.CopyTo(buffer.Slice(pos));
                    pos += seg.Length;
                }

                return new string(buffer);
            }

            var sb = new StringBuilder(totalLen);
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0) sb.Append(KEY_SEPARATOR);
                sb.Append(key.GetSegmentSpan(i));
            }
            return sb.ToString();
        }

        public void Clear()
        {
            cache.Clear();
        }

        public int Count => cache.Count;
    }
}
