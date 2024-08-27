using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;

namespace Minerva.Localizations
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


        public static object GetObject(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            try
            {
                var path1 = ParsePath(obj, path);
                return GetObjectInternal(obj, path1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
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

        private static object GetObjectInternal(object obj, NameEntry[] path, int index = 0)
        {
            if (obj == null) throw new NullReferenceException();
            if (path.Length == 0) return obj;
            var val = GetFieldOrProperty(obj, path[index]);
            if (path.Length == 1)
            {
                return val;
            }
            else
            {
                return GetObjectInternal(val, path, index + 1);
            }
        }

        private static object GetObjectInternalNull(object obj, NameEntry[] path, int index = 0)
        {
            if (obj == null) return null;
            if (path.Length == 0) return obj;
            var val = GetFieldOrProperty(obj, path[index]);
            if (path.Length == 1)
            {
                return val;
            }
            else
            {
                return GetObjectInternalNull(val, path, index + 1);
            }
        }




        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object GetObjectNullPropagation(object obj, ReadOnlyMemory<char> path)
        {
            if (obj == null) return null;
            try
            {
                var splitPath = ParsePath(obj, path);
                return GetObjectNullPropagation(obj, splitPath);
            }
            catch { return null; }
        }

        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static object GetObjectNullPropagation(object obj, NameEntry[] path)
        {
            if (path.Length == 0) return obj;

            try
            {
                return GetObjectInternalNull(obj, path, 0);
            }
            catch (Exception)
            {
                return obj;
            }
        }





        static object GetFieldOrProperty(object obj, NameEntry entry)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return GetValue(obj, entry);
        }

        private static object GetValue(object obj, NameEntry entry)
        {
            object value = null;
            var type = obj.GetType();
            var name = entry.name.ToString();

            if (type.GetField(name) is FieldInfo fieldInfo)
            {
                value = fieldInfo.GetValue(obj);
            }
            else if (type.GetProperty(name) is PropertyInfo propertyInfo)
            {
                value = propertyInfo.GetValue(obj);
            }
            else value = GetFromAttribute(obj, type);

            if (value == null)
            {
                return (object)null;
            }
            if (!entry.index.HasValue)
            {
                return value;
            }
            else
            {
                return value is IList list ? list[entry.index.Value] : null;
            }

            object GetFromAttribute(object obj, Type type)
            {
                var memberInfo = L10nAlias.GetMember(type, name);
                if (memberInfo == null) return null;
                if (memberInfo is FieldInfo field)
                {
                    value = field.GetValue(obj);
                }
                else if (memberInfo is PropertyInfo property)
                {
                    value = property.GetValue(obj);
                }

                return value;
            }
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


    }
}
