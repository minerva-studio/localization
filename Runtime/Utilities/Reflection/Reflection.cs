using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Minerva.Localizations.EscapePatterns;

namespace Minerva.Localizations.Utilities
{
    internal static class Reflection
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private delegate object Getter(object target);

        private readonly struct GetterKey : IEquatable<GetterKey>
        {
            public readonly Type Type;
            public readonly string Name;
            public GetterKey(Type type, string name) { Type = type; Name = name; }
            public bool Equals(GetterKey other) => Type == other.Type && Name == other.Name;
            public override bool Equals(object obj) => obj is GetterKey other && Equals(other);
            public override int GetHashCode() => unchecked((Type.GetHashCode() * 397) ^ Name.GetHashCode());
        }

        private static readonly ConcurrentDictionary<string, L10nPath> pathCache = new();
        private static readonly ConcurrentDictionary<GetterKey, Getter> getterCache = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetObjectNullPropagation(object obj, ReadOnlyMemory<char> path)
        {
            return TryGetObject(obj, path.ToString(), out var value) ? value : null;
        }

        public static object GetObject(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return TryGetObject(obj, path.ToString(), out var value) ? value : null;
        }

        public static bool TryGetObject(object obj, string path, out object value)
        {
            value = null;
            if (obj == null || string.IsNullOrEmpty(path)) return false;
            try
            {
                if (ProxyRegistry.TryGet(obj, path.AsSpan(), out var proxyValue))
                {
                    value = proxyValue;
                    return value != null;
                }

                var parsed = pathCache.GetOrAdd(path, static key => L10nPath.ParseCanonical(key));
                return parsed.TryWalk(obj, 0, ReadOnlySpan<int>.Empty, out value);
            }
            catch
            {
                value = null;
                return false;
            }
        }

        internal static bool TryGetMember(object target, string memberName, out object value)
        {
            value = null;
            if (target == null) return false;
            try
            {
                if (ProxyRegistry.TryGet(target, memberName.AsSpan(), out value)) return value != null;
                var getter = GetOrCreateGetter(target.GetType(), memberName);
                if (getter == null) return false;
                value = getter(target);
                return value != null;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        private static Getter GetOrCreateGetter(Type type, string memberName)
        {
            var key = new GetterKey(type, memberName);
            if (getterCache.TryGetValue(key, out var getter)) return getter;
            MemberInfo member = L10nAlias.GetMember(type, memberName) ?? type.GetProperty(memberName, MemberFlags) ?? (MemberInfo)type.GetField(memberName, MemberFlags);
            getter = CreateGetter(member);
            if (getter == null) return null;
            return getterCache.GetOrAdd(key, getter);
        }

        private static Getter CreateGetter(MemberInfo member)
        {
            if (member is PropertyInfo property)
            {
                var method = property.GetGetMethod(true);
                if (method == null || method.GetParameters().Length != 0) return null;
                return target => method.Invoke(method.IsStatic ? null : target, null);
            }
            if (member is FieldInfo field)
                return target => field.GetValue(field.IsStatic ? null : target);
            return null;
        }
    }
}
