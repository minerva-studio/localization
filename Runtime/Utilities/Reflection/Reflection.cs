using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static System.Linq.Expressions.Expression;

namespace Minerva.Localizations.Utilities
{
    /// <summary>
    /// High-performance reflection system for localization (cross-platform compatible)
    /// </summary>
    internal static class Reflection
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        #region Cache Key

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public readonly Type Type;
            public readonly string Path;

            public CacheKey(Type type, string path)
            {
                Type = type;
                Path = path;
            }

            public bool Equals(CacheKey other)
            {
                return Type == other.Type && Path == other.Path;
            }

            public override bool Equals(object obj)
            {
                return obj is CacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (Type?.GetHashCode() ?? 0);
                    hash = hash * 31 + (Path?.GetHashCode() ?? 0);
                    return hash;
                }
            }

            public override string ToString()
            {
                return $"{Type?.Name}.{Path}";
            }
        }

        #endregion

        #region Entry Struct

        private readonly struct PathEntry : IEquatable<PathEntry>
        {
            public readonly ReadOnlyMemory<char> Name;
            public readonly int Index;
            public readonly bool HasIndex;

            public PathEntry(ReadOnlyMemory<char> name, int index)
            {
                Name = name;
                Index = index;
                HasIndex = true;
            }

            public PathEntry(ReadOnlyMemory<char> name)
            {
                Name = name;
                Index = -1;
                HasIndex = false;
            }

            public bool Equals(PathEntry other)
            {
                return Name.Span.SequenceEqual(other.Name.Span) &&
                       Index == other.Index &&
                       HasIndex == other.HasIndex;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + GetSpanHashCode(Name.Span);
                    hash = hash * 31 + Index;
                    hash = hash * 31 + (HasIndex ? 1 : 0);
                    return hash;
                }
            }

            private static int GetSpanHashCode(ReadOnlySpan<char> span)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var c in span)
                        hash = hash * 31 + c;
                    return hash;
                }
            }
        }

        #endregion

        #region Getter Delegate

        private delegate object Getter(object instance);

        #endregion

        #region Unified Cache

        private readonly struct CacheEntry
        {
            public readonly Getter Getter;
            public readonly MemberInfo MemberInfo;

            public CacheEntry(Getter getter)
            {
                Getter = getter;
                MemberInfo = null;
            }

            public CacheEntry(MemberInfo memberInfo)
            {
                Getter = null;
                MemberInfo = memberInfo;
            }

            public CacheEntry(Getter getter, MemberInfo memberInfo)
            {
                Getter = getter;
                MemberInfo = memberInfo;
            }
        }

        // Unified cache for ALL reflection operations (getters + member info)
        private static readonly ConcurrentDictionary<CacheKey, CacheEntry> s_cache = new();

        #endregion

        #region Public API

        /// <summary>
        /// Get object by path with null propagation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetObjectNullPropagation(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null) return null;
            return GetObject(obj, path);
        }

        /// <summary>
        /// Get object by path (throws if null encountered)
        /// </summary>
        public static object GetObject(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Fast path: check proxy registry
                if (ProxyRegistry.TryGet(obj, path.Span, out var fast))
                    return fast;

                var span = path.Span;

                // Fast path: single property/field access (no '.' or '[')
                if (!span.Contains(".", StringComparison.Ordinal) && !span.Contains("[", StringComparison.Ordinal))
                {
                    return GetSingleMember(obj, path);
                }

                // Parse path to determine complexity
                var entries = ParsePath(obj, path);

                // Fast path: two-level access without indexer (most common)
                if (entries.Length == 2 && !entries[0].HasIndex && !entries[1].HasIndex)
                {
                    return GetTwoLevelMember(obj, entries);
                }

                // Fast path: static path (cacheable)
                if (IsStaticPath(span))
                {
                    var pathStr = path.ToString();
                    var key = new CacheKey(obj.GetType(), pathStr);

                    var entry = s_cache.GetOrAdd(key, k =>
                    {
                        var getter = BuildOptimizedGetter(k.Type, entries);
                        return new CacheEntry(getter);
                    });

                    return entry.Getter?.Invoke(obj);
                }

                // Dynamic path (with dynamic indices) - no cache
                var dynamicGetter = BuildOptimizedGetter(obj.GetType(), entries);
                return dynamicGetter?.Invoke(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        #endregion

        #region Path Parsing (Zero Allocation)

        private static PathEntry[] ParsePath(object obj, ReadOnlyMemory<char> path)
        {
            var span = path.Span;

            // Count segments
            int segmentCount = 1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '.') segmentCount++;
            }

            // Allocate result array
            var entries = new PathEntry[segmentCount];
            int entryIndex = 0;
            int start = 0;

            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == '.')
                {
                    var segment = path.Slice(start, i - start);
                    entries[entryIndex++] = ParseSegment(obj, segment);
                    start = i + 1;
                }
            }

            return entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PathEntry ParseSegment(object obj, ReadOnlyMemory<char> segment)
        {
            var span = segment.Span;

            // Check for indexer: name[index]
            int bracketStart = span.IndexOf('[');
            if (bracketStart == -1)
                return new PathEntry(segment);

            int bracketEnd = span.IndexOf(']');
            if (bracketEnd == -1 || bracketEnd <= bracketStart)
                return new PathEntry(segment);

            // Extract name and index
            var name = segment[..bracketStart];
            var indexSpan = span.Slice(bracketStart + 1, bracketEnd - bracketStart - 1);

            // Try parse as integer
            if (int.TryParse(indexSpan, out int index))
            {
                return new PathEntry(name, index);
            }

            // Dynamic index: resolve it
            var indexObj = GetObjectNullPropagation(obj, segment.Slice(bracketStart + 1, bracketEnd - bracketStart - 1));
            if (TryConvertToInt(indexObj, out int dynamicIndex))
            {
                return new PathEntry(name, dynamicIndex);
            }

            // Failed to parse index
            return new PathEntry(segment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertToInt(object value, out int result)
        {
            switch (value)
            {
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = (int)l;
                    return true;
                case float f:
                    result = (int)f;
                    return true;
                case double d:
                    result = (int)d;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        #endregion

        #region Optimized Getter Building

        private static Getter BuildOptimizedGetter(Type rootType, PathEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
                return static o => o;

            // Fast path: single member
            if (entries.Length == 1 && !entries[0].HasIndex)
            {
                return GetOrCreateGetter(rootType, entries[0].Name.ToString());
            }

            // Fast path: two-level access
            if (entries.Length == 2 && !entries[0].HasIndex && !entries[1].HasIndex)
            {
                return BuildTwoLevelGetter(rootType, entries);
            }

            // Complex path: use expression trees
            return BuildComplexGetter(rootType, entries);
        }

        #endregion

        #region Single Member Access (Fastest Path)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetSingleMember(object obj, ReadOnlyMemory<char> memberName)
        {
            var type = obj.GetType();
            var nameStr = memberName.ToString();

            var getter = GetOrCreateGetter(type, nameStr);
            return getter?.Invoke(obj);
        }

        private static Getter GetOrCreateGetter(Type type, string memberName)
        {
            var key = new CacheKey(type, memberName);
            var entry = s_cache.GetOrAdd(key, static k =>
            {
                var (t, name) = (k.Type, k.Path);

                // Try alias
                var aliasMember = L10nAlias.GetMember(t, name);
                if (aliasMember != null)
                {
                    var getter = CreateDirectGetter(aliasMember);
                    return new CacheEntry(getter, aliasMember);
                }

                // Try property
                var prop = t.GetProperty(name, BF);
                if (prop != null)
                {
                    var getter = CreateDirectGetter(prop);
                    return new CacheEntry(getter, prop);
                }

                // Try field
                var field = t.GetField(name, BF);
                if (field != null)
                {
                    var getter = CreateDirectGetter(field);
                    return new CacheEntry(getter, field);
                }

                return new CacheEntry(null, null);
            });

            return entry.Getter;
        }

        private static Getter CreateDirectGetter(MemberInfo member)
        {
            if (member is PropertyInfo prop)
            {
                var getMethod = prop.GetGetMethod(true);
                if (getMethod == null) return null;

                // Use direct MethodInfo.Invoke for single-member access
                if (getMethod.IsStatic)
                {
                    return obj =>
                    {
                        try { return getMethod.Invoke(null, null); }
                        catch { return null; }
                    };
                }
                else
                {
                    return obj =>
                    {
                        try { return getMethod.Invoke(obj, null); }
                        catch { return null; }
                    };
                }
            }

            if (member is FieldInfo field)
            {
                // FieldInfo.GetValue is optimized in modern .NET
                if (field.IsStatic)
                {
                    return obj =>
                    {
                        try { return field.GetValue(null); }
                        catch { return null; }
                    };
                }
                else
                {
                    return obj =>
                    {
                        try { return field.GetValue(obj); }
                        catch { return null; }
                    };
                }
            }

            return null;
        }

        #endregion

        #region Two-Level Access (Second Fastest Path)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetTwoLevelMember(object obj, PathEntry[] entries)
        {
            var type = obj.GetType();
            var name1 = entries[0].Name.ToString();
            var name2 = entries[1].Name.ToString();
            var pathStr = $"{name1}.{name2}";

            var key = new CacheKey(type, pathStr);
            var entry = s_cache.GetOrAdd(key, k =>
            {
                var getter = BuildTwoLevelGetter(k.Type, entries);
                return new CacheEntry(getter);
            });

            return entry.Getter?.Invoke(obj);
        }

        private static Getter BuildTwoLevelGetter(Type rootType, PathEntry[] entries)
        {
            var name1 = entries[0].Name.ToString();
            var name2 = entries[1].Name.ToString();

            // Get first-level getter
            var getter1 = GetOrCreateGetter(rootType, name1);
            if (getter1 == null)
                return static _ => null;

            // Get second-level type
            var midType = GetMemberType(rootType, name1);
            if (midType == null)
                return static _ => null;

            // Get second-level getter
            var getter2 = GetOrCreateGetter(midType, name2);
            if (getter2 == null)
                return static _ => null;

            // Build specialized two-level getter
            return obj =>
            {
                var mid = getter1(obj);
                if (mid == null) return null;
                return getter2(mid);
            };
        }

        #endregion

        #region Complex Getter Building (Expression Trees)

        private static Getter BuildComplexGetter(Type rootType, PathEntry[] entries)
        {
            // Build lambda: (object o) => o.Prop1.Prop2[index].Prop3
            var param = Parameter(typeof(object), "o");
            Expression current = Convert(param, rootType);
            Type currentType = rootType;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var nameStr = entry.Name.ToString();

                // Build member access expression
                var memberExpr = BuildMemberExpression(current, currentType, nameStr);
                if (memberExpr == null)
                    return static _ => null;

                current = memberExpr;
                currentType = GetMemberType(currentType, nameStr);

                // Apply indexer if needed
                if (entry.HasIndex)
                {
                    // Check if it's IList
                    if (typeof(IList).IsAssignableFrom(currentType))
                    {
                        // current = ((IList)current)[index]
                        var listExpr = Convert(current, typeof(IList));
                        var indexExpr = Constant(entry.Index);
                        current = Property(listExpr, "Item", indexExpr);
                        currentType = typeof(object); // IList returns object
                    }
                    else
                    {
                        // Not indexable
                        return static _ => null;
                    }
                }

                // Null check
                if (i < entries.Length - 1 && !currentType.IsValueType)
                {
                    // if (current == null) return null;
                    var nullCheck = Condition(
                        Equal(current, Constant(null, currentType)),
                        Constant(null, typeof(object)),
                        Convert(current, typeof(object))
                    );
                    current = nullCheck;
                }
            }

            // Box result
            if (currentType.IsValueType)
                current = Convert(current, typeof(object));

            var lambda = Lambda<Func<object, object>>(current, param);
            return new Getter(lambda.Compile());
        }

        private static Expression BuildMemberExpression(Expression instance, Type declaringType, string memberName)
        {
            // Try alias first
            var aliasMember = L10nAlias.GetMember(declaringType, memberName);
            if (aliasMember is PropertyInfo aliasP)
            {
                var getMethod = aliasP.GetGetMethod(true);
                if (getMethod == null) return null;
                return getMethod.IsStatic
                    ? Call(getMethod)
                    : Call(Convert(instance, declaringType), getMethod);
            }
            if (aliasMember is FieldInfo aliasF)
            {
                return aliasF.IsStatic
                    ? Field(null, aliasF)
                    : Field(Convert(instance, declaringType), aliasF);
            }

            // Try property
            var prop = declaringType.GetProperty(memberName, BF);
            if (prop != null)
            {
                var getMethod = prop.GetGetMethod(true);
                if (getMethod == null) return null;
                return getMethod.IsStatic
                    ? Call(getMethod)
                    : Call(Convert(instance, declaringType), getMethod);
            }

            // Try field
            var field = declaringType.GetField(memberName, BF);
            if (field != null)
            {
                return field.IsStatic
                    ? Field(null, field)
                    : Field(Convert(instance, declaringType), field);
            }

            return null;
        }

        #endregion

        #region Type Helpers

        private static Type GetMemberType(Type type, string memberName)
        {
            var key = new CacheKey(type, memberName);
            var entry = s_cache.GetOrAdd(key, k =>
            {
                var (t, name) = (k.Type, k.Path);

                // Try alias
                var aliasMember = L10nAlias.GetMember(t, name);
                if (aliasMember != null)
                    return new CacheEntry(null, aliasMember);

                // Try property
                var prop = t.GetProperty(name, BF);
                if (prop != null)
                    return new CacheEntry(null, prop);

                // Try field
                var field = t.GetField(name, BF);
                return new CacheEntry(null, field);
            });

            if (entry.MemberInfo is PropertyInfo pi) return pi.PropertyType;
            if (entry.MemberInfo is FieldInfo fi) return fi.FieldType;
            return null;
        }

        private static Type GetMemberType(Type type, ReadOnlyMemory<char> nameMem)
        {
            return GetMemberType(type, nameMem.ToString());
        }

        #endregion

        #region Path Analysis

        /// <summary>
        /// Check if path is static (contains only literal indices)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsStaticPath(ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '[')
                {
                    i++; // Skip '['
                    if (i >= span.Length) return false;

                    // Allow optional sign
                    if (span[i] == '-' || span[i] == '+')
                        i++;

                    bool hasDigit = false;
                    while (i < span.Length && char.IsDigit(span[i]))
                    {
                        hasDigit = true;
                        i++;
                    }

                    if (!hasDigit || i >= span.Length || span[i] != ']')
                        return false;
                }
            }
            return true;
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Clear all reflection caches (for testing/debugging)
        /// </summary>
        public static void ClearCaches()
        {
            s_cache.Clear();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public static (int total, int withGetter, int memberInfoOnly) GetCacheStats()
        {
            int withGetter = 0;
            int memberInfoOnly = 0;

            foreach (var kvp in s_cache)
            {
                if (kvp.Value.Getter != null)
                    withGetter++;
                else if (kvp.Value.MemberInfo != null)
                    memberInfoOnly++;
            }

            return (s_cache.Count, withGetter, memberInfoOnly);
        }

        /// <summary>
        /// Get detailed cache information (for debugging)
        /// </summary>
        public static string[] GetCacheDetails()
        {
            var details = new string[s_cache.Count];
            int index = 0;

            foreach (var kvp in s_cache)
            {
                var entry = kvp.Value;
                var status = entry.Getter != null ? "Getter" :
                            entry.MemberInfo != null ? "MemberInfo" :
                            "Empty";
                details[index++] = $"{status}: {kvp.Key}";
            }

            return details;
        }

        #endregion
    }
}