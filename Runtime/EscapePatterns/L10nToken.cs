using System;
using System.Collections.Generic;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Token type for L10n string parsing
    /// </summary>
    internal enum TokenType
    {
        Literal,          // Plain text (including HTML tags)
        KeyReference,     // $key$ or $@key$
        DynamicValue,     // {expr} or {expr:format}
        ColorTag,         // §C...§ or §#FFFFFF...§
    }

    /// <summary>
    /// A token in L10n string
    /// </summary>
    internal sealed class L10nToken
    {
        public TokenType Type { get; set; }
        public ReadOnlyMemory<char> Content { get; set; }
        public ReadOnlyMemory<char> Metadata { get; set; }
        public ReadOnlyMemory<char> Parameters { get; set; }
        public bool IsTooltip { get; set; }

        /// <summary>
        /// Nested tokens (only used for ColorTag)
        /// </summary>
        public List<L10nToken> Children { get; set; }

        public override string ToString()
        {
            var childInfo = Children != null ? $" ({Children.Count} children)" : "";
            var paramInfo = Parameters.Length > 0 ? $" <{Parameters}>" : "";
            return $"{Type}: {Content}{paramInfo}{childInfo}";
        }
    }
}