using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using static System.Linq.Expressions.Expression;

namespace Minerva.Localizations.Utilities
{
    /// <summary>
    /// Simple reflection system used in localization system
    /// <br/>
    /// </summary>
    /// <author>
    /// Author : Wendi Cai
    /// </author>
    internal static class Reflection
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// An reflection entry name
        /// </summary>
        struct NameEntry
        {
            internal ReadOnlyMemory<char> name;
            internal int? index;

            public NameEntry(string name, int? index) : this()
            {
                this.name = name.AsMemory();
                this.index = index;
            }

            public NameEntry(ReadOnlyMemory<char> name, int? index) : this()
            {
                this.name = name;
                this.index = index;
            }
        }

        private static NameEntry[] ParsePath(object obj, ReadOnlyMemory<char> path)
        {
            int count = 1;
            for (int i = 0; i < path.Length; i++)
            {
                if (path.Span[i] == '.') count++;
            }
            NameEntry[] entries = new NameEntry[count];// (count, allocator);

            for (int i = 0; i < count; i++)
            {
                ReadOnlyMemory<char> current;
                // last entry
                if (i == count - 1)
                {
                    current = path;
                }
                else
                {
                    int nextIndex = path.Span.IndexOf('.');
                    current = path[..nextIndex];
                    path = path[(nextIndex + 1)..];
                }

                var result = HandleIndex(obj, current);
                if (!result.HasValue)
                {
                    entries[i] = new NameEntry(current, null);
                }
                else
                {
                    var (index, range) = result.Value;
                    entries[i] = new NameEntry(current[..range.Start], index);
                }
            }

            return entries;
        }



        private static (int, Range)? HandleIndex(object baseObject, ReadOnlyMemory<char> name)
        {
            var nameSpan = name.Span;
            //looks like contains indexer
            if (nameSpan.IndexOf("[") == -1 || nameSpan.IndexOf("]") == -1)
            {
                return null;
            }

            Range indexRange = nameSpan.IndexOf('[')..(nameSpan.IndexOf(']') + 1);
            var indexStr = name[(nameSpan.IndexOf('[') + 1)..nameSpan.IndexOf(']')];
            //Debug.Log(indexStr);
            if (int.TryParse(indexStr.Span, out int index))
            {
                return (index, indexRange);
            }

            var parsedIndex = GetObjectNullPropagation(baseObject, indexStr);
            if (parsedIndex is int i)
            {
                return (i, indexRange);
            }
            else if (parsedIndex is float f)
            {
                return ((int)f, indexRange);
            }
            else if (parsedIndex is long l)
            {
                return ((int)l, indexRange);
            }
            else if (parsedIndex is double d)
            {
                return ((int)d, indexRange);
            }
            else
            {
                return null;
            }
        }

        private delegate object Getter(object instance);

        private static readonly ConcurrentDictionary<(Type, string), Getter> s_getterCache = new();

        public static object GetObject(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            try
            {
                if (ProxyRegistry.TryGet(obj, path.Span, out var fast)) return fast;

                var pathStr = path.ToString();
                bool cacheable = HasOnlyNumericIndices(path.Span);

                var getter = cacheable
                    ? s_getterCache.GetOrAdd((obj.GetType(), pathStr), k =>
                    {
                        var entries = ParsePath(obj, path);
                        return BuildGetter(k.Item1, entries);
                    })
                    : BuildGetter(obj.GetType(), ParsePath(obj, path)); // no cache

                return getter?.Invoke(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public static object GetObjectNullPropagation(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null) return null;
            return GetObject(obj, path);
        }

        private static Getter BuildGetter(Type rootType, NameEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
                return static o => o;

            Func<object, object> chain = static o => o;
            Type currentType = rootType;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                // 1) create member getter
                var segment = CreateMemberGetter(currentType, entry.name);
                if (segment == null) return null;

                // 2) IList indexable
                Func<object, object> segmentWithIndex = segment;
                if (entry.index.HasValue)
                {
                    int idx = entry.index.Value;
                    segmentWithIndex = inst =>
                    {
                        var mid = segment(inst);
                        if (mid == null) return null;
                        if (mid is IList list)
                        {
                            if ((uint)idx < (uint)list.Count) return list[idx];
                            return null;
                        }
                        return null;
                    };
                }

                // null propagate
                var prev = chain;
                chain = inst =>
                {
                    var mid = prev(inst);
                    if (mid == null) return null;
                    return segmentWithIndex(mid);
                };

                // move to next member type
                currentType = GetMemberType(currentType, entry.name);
                if (entry.index.HasValue)
                {
                    // don't know the specific type, set to object for now
                    // actual type will be determined later when we have the result
                    currentType = typeof(object);
                }
                if (currentType == null) return null;
            }

            return new Getter(chain);
        }

        private static Func<object, object> CreateMemberGetter(Type declaringType, ReadOnlyMemory<char> nameMem)
        {

            string name = nameMem.ToString();

            // Alias
            if (L10nAlias.GetMember(declaringType, name) is MemberInfo m)
            {
                if (m is PropertyInfo p2)
                {
                    return BuildProperty(declaringType, p2);
                }
                if (m is FieldInfo f2)
                {
                    return BuildField(declaringType, f2);
                }
            }

            // lookup for field/properties
            if (declaringType.GetProperty(name, BF) is PropertyInfo pi)
            {
                return BuildProperty(declaringType, pi);
            }
            if (declaringType.GetField(name, BF) is FieldInfo fi)
            {
                return BuildField(declaringType, fi);
            }

            return null;
        }

        private static Func<object, object> BuildField(Type declaringType, FieldInfo field)
        {
            var inst = Parameter(typeof(object), "o");
            Expression read = field.IsStatic
                ? Field(null, field)
                : Field(Convert(inst, declaringType), field);

            var box = Convert(read, typeof(object));
            return Lambda<Func<object, object>>(box, inst).Compile();
        }

        private static Func<object, object> BuildProperty(Type declaringType, PropertyInfo property)
        {
            var gm = property.GetGetMethod(true);
            if (gm == null) return null;

            var inst = Parameter(typeof(object), "o");
            Expression call = gm.IsStatic
                ? Call(gm) // static property getter
                : Call(Convert(inst, declaringType), gm);
            var box = Convert(call, typeof(object));
            return Lambda<Func<object, object>>(box, inst).Compile();

        }

        private static Type GetMemberType(Type type, ReadOnlyMemory<char> nameMem)
        {
            string name = nameMem.ToString();
            if (type.GetProperty(name, BF) is PropertyInfo pi) return pi.PropertyType;
            if (type.GetField(name, BF) is FieldInfo fi) return fi.FieldType;

            var alias = L10nAlias.GetMember(type, name);
            if (alias is PropertyInfo p2) return p2.PropertyType;
            if (alias is FieldInfo f2) return f2.FieldType;
            return null;
        }

        private static bool HasOnlyNumericIndices(ReadOnlySpan<char> s)
        {
            int i = 0;
            while (i < s.Length)
            {
                if (s[i++] == '[')
                {
                    if (i >= s.Length) return false;
                    bool hasDigit = false;
                    int j = i;
                    if (s[j] == '-' || s[j] == '+') j++;
                    while (j < s.Length && char.IsDigit(s[j])) { hasDigit = true; j++; }
                    if (!hasDigit) return false;
                    if (j >= s.Length || s[j] != ']') return false;
                    i = j + 1;
                }
            }
            return true;
        }
    }
}
