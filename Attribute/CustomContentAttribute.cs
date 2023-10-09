using System;
using System.Collections.Generic;
using System.Linq;

namespace Minerva.Localizations
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CustomContentAttribute : Attribute
    {
        private static Dictionary<Type, Type> table = new();
        private Type targetType;

        static CustomContentAttribute()
        {
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            {
                if (item.IsAbstract) continue;
                if (GetCustomAttribute(item, typeof(CustomContentAttribute)) is not CustomContentAttribute attr) continue;

                table.Add(attr.targetType, item);
            }
            table[typeof(object)] = typeof(GenericL10nContext);
            table[typeof(Enum)] = typeof(EnumL10nContext);
        }

        public CustomContentAttribute(Type targetType)
        {
            this.targetType = targetType;
        }

        /// <summary>
        /// Get content type
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static Type GetContentType(Type targetType, bool allowDefault = false)
        {
            if (table.TryGetValue(targetType, out var result))
            {
                return result;
            }
            if (targetType.IsEnum)
            {
                return table[typeof(Enum)];
            }
            return allowDefault ? table[typeof(object)] : null;
        }
    }
}
