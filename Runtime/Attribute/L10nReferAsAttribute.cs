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
        public struct TypeEntry
        {
            public string? TypeName;
            public Key? TypeKey;

            public bool MemberMapBuilt;
            public Dictionary<string, MemberInfo>? MemberMap;
        }

        private static readonly Dictionary<Type, TypeEntry> entries = new();

        public static string GetTypeName(Type type)
        {
            if (entries.TryGetValue(type, out var entry) && entry.TypeName != null)
            {
                return entry.TypeName;
            }

            string name;
            if (Attribute.GetCustomAttribute(type, typeof(L10nReferAsAttribute)) is L10nReferAsAttribute attr)
            {
                name = attr.Key;
            }
            else
            {
                name = type.FullName;
            }

            if (!entries.TryGetValue(type, out entry))
            {
                entry = default;
            }

            entry.TypeName = name;
            entries[type] = entry;
            return name;
        }

        public static Key GetTypeKey(Type type)
        {
            if (entries.TryGetValue(type, out var entry) && entry.TypeKey.HasValue)
            {
                return entry.TypeKey.Value;
            }

            var key = new Key(GetTypeName(type));

            if (!entries.TryGetValue(type, out entry))
            {
                entry = default;
            }

            entry.TypeKey = key;
            entries[type] = entry;
            return key;
        }

        public static MemberInfo? GetMember(Type type, string name)
        {
            if (!entries.TryGetValue(type, out var entry))
            {
                entry = default;
            }

            if (!entry.MemberMapBuilt)
            {
                entry.MemberMap = BuildMap(type);
                entry.MemberMapBuilt = true;
                entries[type] = entry;
            }

            if (entry.MemberMap == null)
            {
                return null;
            }

            return entry.MemberMap.TryGetValue(name, out var member) ? member : null;
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
