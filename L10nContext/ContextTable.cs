using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Minerva.Localizations
{
    /// <summary>
    /// Table of defined context type for each type of object
    /// </summary>
    internal class ContextTable
    {
        interface IContextFactory<in T>
        {
            L10nContext Create(T t);
        }

        delegate L10nContext ContextFactory<in T>(T baseValue);

        private static Dictionary<Type, (Type type, Func<L10nContext> builder, bool allowInheritance)> table;

        static ContextTable()
        {
            Init();
        }

        private static void Init()
        {
            table = new();
            Register<GenericL10nContext, object>();
            Register<EnumL10nContext, Enum>();
            Register<KeyL10nContext, string>();
        }

        internal static void Register<TContext, TTarget>(bool allowInheritance = false) where TContext : L10nContext, new()
        {
            var targetType = typeof(TTarget);
            // duplicate keys, likely some error from user input
            if (table.TryGetValue(targetType, out var existing))
            {
                (_, _, var inherit) = existing;
                // use the one that support inheritance
                if (inherit != allowInheritance && allowInheritance)
                {
                    table[targetType] = (typeof(TContext), Constructor<TContext>, true);
                }
            }
            else table.Add(targetType, (typeof(TContext), Constructor<TContext>, allowInheritance));
        }

        static T Constructor<T>() where T : new()
        {
            return new T();
        }

        /// <summary>
        /// Get context type for <paramref name="valueType"/>, considering parent
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static Type GetContextType(Type valueType)
        {
            return GetContextData(valueType).type;
        }

        /// <summary>
        /// Get context builder for <paramref name="valueType"/>, considering parent
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static Func<L10nContext> GetContextBuilder(Type valueType)
        {
            return GetContextData(valueType).builder;
        }

        /// <summary>
        /// Get context type for <paramref name="valueType"/>, considering parent
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static (Type type, Func<L10nContext> builder) GetContextData(Type valueType)
        {
            var currType = valueType;
            // try get from class inheritance
            while (currType != null)
            {
                if (table.TryGetValue(currType, out var result) && (result.allowInheritance || currType == valueType))
                {
                    // result is in 
                    return (result.type, result.builder);
                }
                currType = currType.BaseType;
            }
            // try get from interface implement 
            foreach (var interfaceInfo in valueType.GetInterfaces())
            {
                currType = interfaceInfo;
                if (table.TryGetValue(currType, out var result) && (result.allowInheritance || currType == valueType))
                {
                    // result is in 
                    return (result.type, result.builder);
                }
            }
            return (typeof(GenericL10nContext), () => new GenericL10nContext());
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

        /// <summary>
        /// Get context type for <paramref name="valueType"/>, considering parent
        /// </summary>
        /// <remarks>
        /// Strings and Enums are always considered context defined type
        /// </remarks>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static bool HasContextConstructorDefined(Type valueType, out Func<L10nContext> contextType)
        {
            contextType = null;
            var currType = valueType;
            while (currType != null)
            {
                if (table.TryGetValue(currType, out var result) && (result.allowInheritance || currType == valueType))
                {
                    contextType = result.builder;
                    return true;
                }
                currType = currType.BaseType;
            }
            return false;
        }
    }
}
