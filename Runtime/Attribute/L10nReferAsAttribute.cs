using System;
using System.Collections.Generic;
using System.Reflection;

namespace Minerva.Localizations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class L10nReferAsAttribute : Attribute
    {
        private readonly string key;

        public string Key => key;

        public L10nReferAsAttribute(string key)
        {
            this.key = key;
        }

    }

#nullable enable 
    public static class L10nAlias
    {
        public static readonly Dictionary<Type, string> typeName = new();
        public static readonly Dictionary<Type, Dictionary<string, MemberInfo>?> keyAttributes = new();

        public static string GetTypeName(Type type)
        {
            if (typeName.TryGetValue(type, out var name))
                return name;
            if (Attribute.GetCustomAttribute(type, typeof(L10nReferAsAttribute)) is L10nReferAsAttribute attr)
                return typeName[type] = attr.Key;
            else
                return typeName[type] = type.FullName;
        }

        public static MemberInfo? GetMember(Type type, string name)
        {
            if (!keyAttributes.TryGetValue(type, out var map))
            {
                map = BuildMap(type);
                keyAttributes[type] = map;
            }
            if (map == null) return null;
            return map.TryGetValue(name, out var member) ? member : null;
        }

        private static Dictionary<string, MemberInfo>? BuildMap(Type type)
        {
            Dictionary<string, MemberInfo>? map = null;
            foreach (var item in type.GetMembers())
            {
                if (Attribute.GetCustomAttribute(item, typeof(L10nReferAsAttribute)) is L10nReferAsAttribute attr)
                {
                    map ??= new Dictionary<string, MemberInfo>();
                    map[attr.Key] = item;
                }
            }
            return map;
        }
    }
}
