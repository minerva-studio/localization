using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Table of defined context type for each type of object
    /// </summary>
    internal class ContextTable
    {
        private static Dictionary<Type, (Type type, bool allowInheritance)> table;

        static ContextTable()
        {
            Init();
        }

        private static void Init()
        {
            table = new()
            {
                [typeof(object)] = (typeof(GenericL10nContext), true),
                [typeof(Enum)] = (typeof(EnumL10nContext), true),
                [typeof(string)] = (typeof(KeyL10nContext), true)
            };
        }

        internal static void Register(Type contextType, Type targetType, bool allowInheritance = false)
        {
            // duplicate keys, likely some error from user input
            if (table.ContainsKey(targetType))
            {
                var (type, inherit) = table[targetType];
                // use the one that support inheritance
                if (inherit != allowInheritance && allowInheritance)
                {
                    table[targetType] = (contextType, true);
                }
            }
            else table.Add(targetType, (contextType, allowInheritance));
        }

        internal static void Register<TContext, TTarget>(bool allowInheritance = false) where TContext : L10nContext
        {
            Register(typeof(TContext), typeof(TTarget), allowInheritance);
        }


        /// <summary>
        /// Get context type for <paramref name="valueType"/>, considering parent
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static Type GetContextType(Type valueType)
        {
            var currType = valueType;
            while (currType != null)
            {
                if (table.TryGetValue(currType, out var result) && (result.allowInheritance || currType == valueType))
                {
                    return result.type;
                }
                currType = currType.BaseType;
            }
            return typeof(GenericL10nContext);
        }

        /// <summary>
        /// Get context type for <paramref name="valueType"/>, considering parent
        /// </summary>
        /// <remarks>
        /// Strings and Enums are always considered context defined type
        /// </remarks>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static bool HasContextTypeDefined(Type valueType, out Type contextType)
        {
            contextType = null;
            var currType = valueType;
            while (currType != null)
            {
                if (table.TryGetValue(currType, out var result) && (result.allowInheritance || currType == valueType))
                {
                    contextType = result.type;
                    return true;
                }
                currType = currType.BaseType;
            }
            return false;
        }
    }
}
