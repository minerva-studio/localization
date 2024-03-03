using System;
using System.Linq;

namespace Minerva.Localizations
{
    [Obsolete("Custom context attribute is outdated now", true)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CustomContextAttribute : Attribute
    {
        private Type targetType;
        private bool inherit;

        public static void Init()
        {
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            {
                if (item.IsAbstract) continue;
                if (GetCustomAttribute(item, typeof(CustomContextAttribute)) is not CustomContextAttribute attr) continue;
                ContextTable.Register(item, attr.targetType, attr.inherit);
            }
        }

        public CustomContextAttribute(Type targetType, bool inherit = true)
        {
            this.targetType = targetType;
            this.inherit = inherit;
        }
    }
}
