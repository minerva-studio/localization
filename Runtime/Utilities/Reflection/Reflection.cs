using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Minerva.Localizations.EscapePatterns;

namespace Minerva.Localizations.Utilities
{
    internal static class Reflection
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        private abstract class MemberGetter
        {
            public abstract L10nValue Get(object target);
        }

        private sealed class BoxedGetter : MemberGetter
        {
            private readonly MethodInfo method;
            private readonly FieldInfo field;
            public BoxedGetter(MethodInfo method) => this.method = method;
            public BoxedGetter(FieldInfo field) => this.field = field;
            public override L10nValue Get(object target) => L10nValue.FromObject(method != null
                ? method.Invoke(method.IsStatic ? null : target, null)
                : field.GetValue(field.IsStatic ? null : target));
        }

        private sealed class PropertyGetter<TTarget, TValue> : MemberGetter where TTarget : class
        {
            private readonly Func<TTarget, TValue> getter;
            public PropertyGetter(MethodInfo method) => getter = (Func<TTarget, TValue>)Delegate.CreateDelegate(typeof(Func<TTarget, TValue>), method);
            public override L10nValue Get(object target) => ValueConversion<TValue>.Convert(getter((TTarget)target));
        }

        private static class ValueConversion<T>
        {
            public static L10nValue Convert(T value)
            {
                if (typeof(T) == typeof(int))
                {
                    int v = UnsafeUtility.As<T, int>(ref value);
                    return L10nValue.FromNumber(v);
                }
                if (typeof(T) == typeof(long))
                {
                    long v = UnsafeUtility.As<T, long>(ref value);
                    return L10nValue.FromNumber(v);
                }
                if (typeof(T) == typeof(float))
                {
                    float v = UnsafeUtility.As<T, float>(ref value);
                    return L10nValue.FromNumber(v);
                }
                if (typeof(T) == typeof(double))
                {
                    double v = UnsafeUtility.As<T, double>(ref value);
                    return L10nValue.FromNumber(v);
                }
                if (typeof(T) == typeof(bool))
                {
                    bool v = UnsafeUtility.As<T, bool>(ref value);
                    return L10nValue.FromNumber(v ? 1 : 0);
                }
                if (typeof(T) == typeof(short))
                {
                    short v = UnsafeUtility.As<T, short>(ref value);
                    return L10nValue.FromNumber(v);
                }
                if (typeof(T) == typeof(byte))
                {
                    byte v = UnsafeUtility.As<T, byte>(ref value);
                    return L10nValue.FromNumber(v);
                }
                return L10nValue.FromObject(value);
            }
        }

        private readonly struct GetterKey : IEquatable<GetterKey>
        {
            public readonly Type Type;
            public readonly string Name;
            public GetterKey(Type type, string name) { Type = type; Name = name; }
            public bool Equals(GetterKey other) => Type == other.Type && Name == other.Name;
            public override bool Equals(object obj) => obj is GetterKey other && Equals(other);
            public override int GetHashCode() => unchecked((Type.GetHashCode() * 397) ^ Name.GetHashCode());
        }

        private const int MaximumPathCacheEntries = 4096;
        private static readonly BoundedConcurrentCache<string, L10nPath> pathCache = new(MaximumPathCacheEntries);
        // Getter keys are bounded by loaded runtime types and source-authored member names; failed lookups are not cached.
        // This assumes the application does not continually generate new runtime types.
        private static readonly ConcurrentDictionary<GetterKey, MemberGetter> getterCache = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetObjectNullPropagation(object obj, ReadOnlyMemory<char> path)
            => TryGetObject(obj, path.ToString(), out var value) ? value.ToObject() : null;

        public static object GetObject(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return TryGetObject(obj, path.ToString(), out var value) ? value.ToObject() : null;
        }

        public static bool TryGetObject(object obj, string path, out L10nValue value)
        {
            value = default;
            if (L10nValue.IsMissing(obj) || string.IsNullOrEmpty(path)) return false;
            try
            {
                if (ProxyRegistry.TryGet(obj, path.AsSpan(), out var proxyValue))
                {
                    value = L10nValue.FromObject(proxyValue);
                    return !value.IsNull;
                }
                if (!pathCache.TryGetValue(path, out var parsed))
                    parsed = pathCache.GetOrAdd(path, L10nPath.ParseCanonical(path));
                return parsed.TryWalk(L10nValue.FromObject(obj), 0, ReadOnlySpan<int>.Empty, out value);
            }
            catch { value = default; return false; }
        }

        internal static bool TryGetMember(object target, string memberName, out L10nValue value)
        {
            value = default;
            if (L10nValue.IsMissing(target)) return false;
            try
            {
                if (ProxyRegistry.TryGet(target, memberName.AsSpan(), out var proxyValue))
                {
                    value = L10nValue.FromObject(proxyValue);
                    return !value.IsNull;
                }
                var getter = GetOrCreateGetter(target.GetType(), memberName);
                if (getter == null) return false;
                value = getter.Get(target);
                return !value.IsNull;
            }
            catch { value = default; return false; }
        }

        private static MemberGetter GetOrCreateGetter(Type type, string memberName)
        {
            var key = new GetterKey(type, memberName);
            if (getterCache.TryGetValue(key, out var getter)) return getter;
            MemberInfo member = L10nAlias.GetMember(type, memberName)
                                ?? type.GetProperty(memberName, MemberFlags)
                                ?? (MemberInfo)type.GetField(memberName, MemberFlags);
            getter = CreateGetter(member);
            return getter == null ? null : getterCache.GetOrAdd(key, getter);
        }

        private static MemberGetter CreateGetter(MemberInfo member)
        {
            if (member is PropertyInfo property)
            {
                MethodInfo method = property.GetGetMethod(true);
                if (method == null || method.GetParameters().Length != 0) return null;
                Type targetType = method.DeclaringType;
                Type valueType = property.PropertyType;
                if (!method.IsStatic && targetType.IsClass && IsSupportedPropertyType(valueType))
                {
                    try
                    {
                        Type genericValueType = IsScalarPropertyType(valueType) ? valueType : typeof(object);
                        Type getterType = typeof(PropertyGetter<,>).MakeGenericType(targetType, genericValueType);
                        return (MemberGetter)Activator.CreateInstance(getterType, method);
                    }
                    catch { }
                }
                return new BoxedGetter(method);
            }
            if (member is FieldInfo field) return new BoxedGetter(field);
            return null;
        }

        private static bool IsScalarPropertyType(Type type) =>
            type == typeof(int) || type == typeof(long) || type == typeof(float) ||
            type == typeof(double) || type == typeof(bool) || type == typeof(short) ||
            type == typeof(byte);
        private static bool IsSupportedPropertyType(Type type) => IsScalarPropertyType(type) || !type.IsValueType;
    }
}
