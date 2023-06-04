using System;

namespace Amlos.Localizations
{
    internal class KeyNameAttribute : Attribute
    {
        private readonly string key;

        public string Key => key;

        public KeyNameAttribute(string key)
        {
            this.key = key;
        }

    }
}
