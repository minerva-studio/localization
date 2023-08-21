using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
            internal string name;
            internal int? index;

            public NameEntry(string name, int? index) : this()
            {
                this.name = name;
                this.index = index;
            }
        }



        public static object GetObject(object obj, string path)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            try
            {
                NameEntry[] path1 = ParsePath(obj, path);
                return GetObjectInternal(obj, path1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static NameEntry[] ParsePath(object obj, string path)
        {
            string[] splitPath = path.Split('.');
            NameEntry[] entries = new NameEntry[splitPath.Length];
            for (int i = 0; i < splitPath.Length; i++)
            {
                var index = HandleIndex(obj, splitPath[i]);
                if (!index.HasValue)
                {
                    entries[i] = new NameEntry(splitPath[i], null);
                    continue;
                }
                var name = splitPath[i][..index.Value.Item2.Start] + splitPath[i][index.Value.Item2.End..];
                entries[i] = new NameEntry(name, index.Value.Item1);
            }

            return entries;
        }

        private static object GetObjectInternal(object obj, NameEntry[] path)
        {
            if (path.Length == 0) return obj;


            var val = GetFieldOrProperty(obj, path[0]);
            if (path.Length == 1)
            {
                return val;
            }
            else
            {
                return GetObjectInternal(val, path[1..]);
            }
        }




        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static object GetObjectNullPropagation(object obj, string path)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            try
            {
                NameEntry[] path1 = ParsePath(obj, path);
                return GetObjectNullPropagation(obj, path1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
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


            var val = GetFieldOrProperty(obj, path[0]);
            if (path.Length == 1) return val;

            try
            {
                object v = GetObjectInternal(val, path[1..]);
                return v ?? obj;
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
            foreach (var item in type.GetMembers().Where(predicate))
            {
                if (item is FieldInfo field)
                {
                    value = field.GetValue(obj);
                    break;
                }
                else if (item is PropertyInfo property)
                {
                    value = property.GetValue(obj);
                    break;
                }
            }

            if (value == null)
            {
                foreach (var item in type.GetMember(entry.name))
                {
                    if (item is FieldInfo field)
                    {
                        value = field.GetValue(obj);
                        break;
                    }
                    else if (item is PropertyInfo property)
                    {
                        value = property.GetValue(obj);
                        break;
                    }
                }

            }

            if (value != null)
            {
                if (!entry.index.HasValue)
                {
                    return value;
                }
                else
                {
                    return value is IList list ? list[entry.index.Value] : null;
                }
            }
            else
            {
                return (object)null;
            }


            bool predicate(MemberInfo t)
            {
                if (t == null) return false;
                KeyNameAttribute attr = (KeyNameAttribute)Attribute.GetCustomAttribute(t, typeof(KeyNameAttribute));
                return attr?.Key == entry.name == true;
            }
        }





        private static (int, Range)? HandleIndex(object baseObject, string name)
        {
            //looks like contains indexer
            if (!name.Contains("[") || !name.Contains("]"))
            {
                return null;
            }

            Range indexRange = name.IndexOf('[')..(name.IndexOf(']') + 1);
            string indexStr = name[(name.IndexOf('[') + 1)..name.IndexOf(']')];
            //Debug.Log(indexStr);
            if (int.TryParse(indexStr, out int index))
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
