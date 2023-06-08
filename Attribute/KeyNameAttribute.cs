using System;

namespace Amlos.Localizations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class KeyNameAttribute : Attribute
    {
        private readonly string key;

        public string Key => key;

        public KeyNameAttribute(string key)
        {
            this.key = key;
        }

    }
}
