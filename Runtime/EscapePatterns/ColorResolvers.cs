using System;
using System.Collections.Generic;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Extension hooks that let the host project plug colour resolvers into the
    /// L10n parser without the core package depending on game-specific types.
    /// Resolvers are tried in registration order; the first non-null result wins.
    /// </summary>
    public static class ColorResolvers
    {
        private static readonly List<Func<string, string>> resolvers = new();

        /// <summary>
        /// Register a resolver that maps a case-sensitive identifier (e.g. <c>Fire</c>,
        /// <c>Legendary</c>) to a TMP hex colour such as <c>#RRGGBB</c>. Return
        /// <c>null</c> when the identifier is not recognised by this resolver.
        /// </summary>
        public static void Register(Func<string, string> resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            resolvers.Add(resolver);
        }

        /// <summary>
        /// Resolve <paramref name="name"/> to a TMP hex string by trying each registered
        /// resolver in order. Returns the first non-null result, or <c>null</c> if none match.
        /// </summary>
        public static string Resolve(string name)
        {
            for (int i = 0; i < resolvers.Count; i++)
            {
                var hex = resolvers[i]?.Invoke(name);
                if (!string.IsNullOrEmpty(hex)) return hex;
            }
            return null;
        }

        /// <summary>
        /// Remove all registered resolvers. Intended for test setup/teardown.
        /// </summary>
        public static void Clear() => resolvers.Clear();
    }
}
