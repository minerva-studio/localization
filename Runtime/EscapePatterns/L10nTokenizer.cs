using System;
using System.Collections.Generic;
using System.Text;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// High-performance tokenizer for L10n strings (single-pass with nesting support)
    /// Supports object pooling for reduced GC pressure
    /// </summary>
    internal sealed class L10nTokenizer
    {
        private ReadOnlyMemory<char> source;
        private int position;

        public L10nTokenizer(string source)
        {
            this.source = (source ?? string.Empty).AsMemory();
            this.position = 0;
        }

        public L10nTokenizer(ReadOnlyMemory<char> source)
        {
            this.source = source;
            this.position = 0;
        }

        /// <summary>
        /// Reset tokenizer for reuse from pool
        /// </summary>
        internal void Reset(ReadOnlyMemory<char> newSource)
        {
            this.source = newSource;
            this.position = 0;
        }

        /// <summary>
        /// Tokenize entire string in one pass
        /// </summary>
        public L10nToken Tokenize()
        {
            var tokens = L10nObjectPool.RentTokenList();
            var literalBuffer = L10nObjectPool.RentStringBuilder();

            try
            {
                while (position < source.Length)
                {
                    char current = Peek();

                    // 1. Escape sequences: \\ \$ \{ \§
                    if (current == '\\' && position + 1 < source.Length)
                    {
                        position++; // Skip '\'
                        literalBuffer.Append(Peek());
                        position++;
                        continue;
                    }

                    // 2. Key reference: $...$ or $@...$
                    if (current == '$' && TryReadKeyReference(out var keyToken))
                    {
                        FlushLiteral(tokens, literalBuffer);
                        tokens.Add(keyToken);
                        continue;
                    }

                    // 3. Dynamic value: {...}
                    if (current == '{' && TryReadDynamicValue(out var dynToken))
                    {
                        FlushLiteral(tokens, literalBuffer);
                        tokens.Add(dynToken);
                        continue;
                    }

                    // 4. Color tag: §C...§ or §#FFFFFF...§
                    if (current == '§' && TryReadColorTag(out var colorToken))
                    {
                        FlushLiteral(tokens, literalBuffer);
                        tokens.Add(colorToken);
                        continue;
                    }

                    literalBuffer.Append(current);
                    position++;
                }

                FlushLiteral(tokens, literalBuffer);

                var rootToken = L10nObjectPool.RentToken();
                rootToken.Type = TokenType.Literal;
                rootToken.Children = tokens;

                return rootToken;
            }
            finally
            {
                L10nObjectPool.ReturnStringBuilder(literalBuffer);
                // Don't return tokens list - it's now owned by rootToken
            }
        }

        #region Token Readers

        private bool TryReadKeyReference(out L10nToken token)
        {
            int start = position;

            if (!Match('$'))
            {
                token = null;
                return false;
            }

            bool isTooltip = Match('@');
            int contentStart = position;

            // Read until next unescaped $
            while (!IsAtEnd() && Peek() != '$')
            {
                if (Peek() == '\\') position++; // Skip escape
                position++;
            }

            if (!Match('$'))
            {
                // Not a valid key reference, rollback
                position = start;
                token = null;
                return false;
            }

            var content = source.Slice(contentStart, position - contentStart - 1);
            token = L10nObjectPool.RentToken();
            token.Type = TokenType.KeyReference;
            token.Content = content;
            token.IsTooltip = isTooltip;
            return true;
        }

        private bool TryReadDynamicValue(out L10nToken token)
        {
            int start = position;

            if (!Match('{'))
            {
                token = null;
                return false;
            }

            int depth = 1;
            int contentStart = position;
            int formatStart = -1;

            while (!IsAtEnd() && depth > 0)
            {
                char c = Peek();

                if (c == '\\')
                {
                    position += 2;
                    continue;
                }

                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) break;
                }
                else if (c == ':' && depth == 1 && formatStart == -1)
                {
                    formatStart = position;
                }

                position++;
            }

            if (depth != 0 || !Match('}'))
            {
                position = start;
                token = null;
                return false;
            }

            ReadOnlyMemory<char> content;
            ReadOnlyMemory<char> format = default;

            if (formatStart > 0)
            {
                content = source.Slice(contentStart, formatStart - contentStart);
                format = source.Slice(formatStart + 1, position - formatStart - 2);
            }
            else
            {
                content = source.Slice(contentStart, position - contentStart - 1);
            }

            token = L10nObjectPool.RentToken();
            token.Type = TokenType.DynamicValue;
            token.Content = content;
            token.Metadata = format;
            return true;
        }

        private bool TryReadColorTag(out L10nToken token)
        {
            int start = position;

            if (!Match('§'))
            {
                token = null;
                return false;
            }

            ReadOnlyMemory<char> colorCode;

            // Try read hex color: #RRGGBB
            if (Peek() == '#')
            {
                position++; // Skip '#'
                int hexStart = position;

                // Read 6 hex digits
                for (int i = 0; i < 6; i++)
                {
                    if (!IsAtEnd() && IsHexDigit(Peek()))
                    {
                        position++;
                    }
                    else
                    {
                        // Not a valid hex color, rollback
                        position = start;
                        token = null;
                        return false;
                    }
                }

                colorCode = source.Slice(hexStart - 1, 7);
            }
            // Try read single char color: R, G, B, etc.
            else if (!IsAtEnd() && char.IsLetter(Peek()))
            {
                colorCode = source.Slice(position, 1);
                position++;
            }
            else
            {
                // Not a valid color code, rollback
                position = start;
                token = null;
                return false;
            }

            int contentStart = position;
            int contentEnd = FindClosingColorTag(contentStart);

            if (contentEnd == -1)
            {
                // No closing §, rollback
                position = start;
                token = null;
                return false;
            }

            var innerContent = source[contentStart..contentEnd];
            var nestedTokenizer = L10nObjectPool.RentTokenizer(innerContent);

            try
            {
                token = nestedTokenizer.Tokenize();
                position = contentEnd + 1;

                token.Type = TokenType.ColorTag;
                token.Content = innerContent;
                token.Metadata = colorCode;
                return true;
            }
            finally
            {
                L10nObjectPool.ReturnTokenizer(nestedTokenizer);
            }
        }

        /// <summary>
        /// Find the closing § for a color tag
        /// ✅ § tags do NOT support self-nesting (use HTML color tags for nested colors)
        /// </summary>
        private int FindClosingColorTag(int startPos)
        {
            int pos = startPos;

            ReadOnlySpan<char> span = source.Span;
            while (pos < source.Length)
            {
                char c = span[pos];

                // Skip escaped characters
                if (c == '\\' && pos + 1 < source.Length)
                {
                    pos += 2;
                    continue;
                }

                if (c == '§')
                {
                    return pos;
                }

                pos++;
            }

            return -1;  // No closing § found
        }

        #endregion

        #region Helpers

        private void FlushLiteral(List<L10nToken> tokens, StringBuilder buffer)
        {
            if (buffer.Length > 0)
            {
                var token = L10nObjectPool.RentToken();
                token.Type = TokenType.Literal;
                token.Content = buffer.ToString().AsMemory();
                tokens.Add(token);
                buffer.Clear();
            }
        }

        private char Peek()
        {
            return position < source.Length ? source.Span[position] : '\0';
        }

        private bool Match(char expected)
        {
            if (IsAtEnd() || Peek() != expected)
                return false;

            position++;
            return true;
        }

        private bool IsAtEnd()
        {
            return position >= source.Length;
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }

        #endregion
    }
}