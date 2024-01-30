using System;
using System.Collections.Generic;
using System.Linq;

namespace Minerva.Localizations
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CustomContextAttribute : Attribute
    {
        private static Dictionary<Type, (Type type, bool allowInheritance)> table = new();
        private Type targetType;
        private bool inherit;

        static CustomContextAttribute()
        {
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            {
                if (item.IsAbstract) continue;
                if (GetCustomAttribute(item, typeof(CustomContextAttribute)) is not CustomContextAttribute attr) continue;
                // duplicate keys, likely some error from user input
                if (table.ContainsKey(attr.targetType))
                {
                    var (type, inherit) = table[attr.targetType];
                    // use the one that support inheritance
                    if (inherit != attr.inherit && attr.inherit)
                    {
                        table[attr.targetType] = (item, true);
                    }
                }
                else table.Add(attr.targetType, (item, attr.inherit));
            }
            table ??= new();
            table[typeof(object)] = (typeof(GenericL10nContext), true);
            table[typeof(Enum)] = (typeof(EnumL10nContext), true);
            table[typeof(string)] = (typeof(KeyL10nContext), true);
        }

        public CustomContextAttribute(Type targetType, bool inherit = true)
        {
            this.targetType = targetType;
            this.inherit = true;
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
