using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEditor.Progress;

namespace Minerva.Localizations
{
    /// <summary>
    /// Simple reflection system used in localization system
    /// <br/>
    /// </summary>
    /// <author>
    /// Author : Wendi Cai
    /// </author>
    public static class Reflection
    {
        public static object GetObject(object obj, string path)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            try
            {
                return GetObjectInternal(obj, path.Split('.'));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public static object GetObject(object obj, string[] path)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            try
            {
                return GetObjectInternal(obj, path);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static object GetObjectInternal(object obj, string[] path)
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
                return GetObjectNullPropagation(obj, path.Split('.'));
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
        private static object GetObjectNullPropagation(object obj, string[] path)
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





        public static object GetFieldOrProperty(object obj, string name)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            //List<MemberInfo> members = GetMembers(obj, name);
            //foreach (var item in members)
            //{
            //    if (item is FieldInfo field)
            //    {
            //        return field.GetValue(obj);
            //    }
            //    else if (item is PropertyInfo property)
            //    {
            //        return property.GetValue(obj);
            //    }
            //}
            return GetValue(obj, name);
        }

        private static List<MemberInfo> GetMembers(object obj, string name)
        {
            var list = new List<MemberInfo>();
            var type = obj.GetType();
            IEnumerable<MemberInfo> collection = type.GetMembers().Where(predicate);
            list.AddRange(collection);
            list.AddRange(type.GetMember(name));

            return list;

            bool predicate(MemberInfo t)
            {
                if (t == null) return false;
                if (Attribute.IsDefined(t, typeof(KeyNameAttribute))) return false;
                return ((KeyNameAttribute)Attribute.GetCustomAttribute(t, typeof(KeyNameAttribute))).Key == name;
            }
        }

        private static object GetValue(object obj, string name)
        {
            var type = obj.GetType();
            foreach (var item in type.GetMembers().Where(predicate))
            {
                if (item is FieldInfo field)
                {
                    return field.GetValue(obj);
                }
                else if (item is PropertyInfo property)
                {
                    return property.GetValue(obj);
                }
            }
            foreach (var item in type.GetMember(name))
            {
                if (item is FieldInfo field)
                {
                    return field.GetValue(obj);
                }
                else if (item is PropertyInfo property)
                {
                    return property.GetValue(obj);
                }
            }

            return null;

            bool predicate(MemberInfo t)
            {
                if (t == null) return false;
                KeyNameAttribute attr = (KeyNameAttribute)Attribute.GetCustomAttribute(t, typeof(KeyNameAttribute));
                return attr?.Key == name == true;
            }
        }
    }
}
