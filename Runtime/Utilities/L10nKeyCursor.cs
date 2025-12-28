using System;
using System.Runtime.CompilerServices;
using static Minerva.Localizations.L10nSymbols;

namespace Minerva.Localizations.Utilities
{
    internal ref struct L10nKeyCursor
    {
        private readonly ReadOnlySpan<char> span;
        private int index;

        public L10nKeyCursor(ReadOnlySpan<char> span)
        {
            this.span = span;
            index = 0;
        }

        public bool TryNext(out ReadOnlySpan<char> segment)
        {
            if ((uint)index >= (uint)span.Length)
            {
                segment = default;
                return false;
            }

            int start = index;
            while ((uint)index < (uint)span.Length && span[index] != KEY_SEPARATOR)
            {
                index++;
            }

            segment = span.Slice(start, index - start);

            if ((uint)index < (uint)span.Length && span[index] == KEY_SEPARATOR)
            {
                index++;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountSegments(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return 0;

            int count = 1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == KEY_SEPARATOR) count++;
            }

            return count;
        }
    }
}
