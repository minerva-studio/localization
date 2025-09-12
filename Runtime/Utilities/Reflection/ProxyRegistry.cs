using System;

namespace Minerva.Localizations.Utilities
{
    public static class ProxyRegistry
    {
        static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, IReflectionProxy> _map = new();

        public static void Register<T>(IReflectionProxy proxy) => _map[typeof(T)] = proxy;

        public static bool TryGet(object target, ReadOnlySpan<char> path, out object value)
        {
            value = null;
            if (target == null) return true;
            if (_map.TryGetValue(target.GetType(), out var p))
                return p.TryGet(target, path, out value);
            return false;
        }
    }
}
