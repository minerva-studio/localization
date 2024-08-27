using System;
using System.Reflection;

namespace Minerva.Localizations
{
    /// <summary>
    /// Default implementation of localization object
    /// </summary>
    public sealed class GenericL10nContext : L10nContext
    {
        protected override void Parse(object value)
        {
            if (value != null)
            {
                System.Type type = value.GetType();
                BaseKey = L10nAlias.GetTypeName(type);
                //_ = TryFindField(value, type, "name")
                //    || TryFindField(value, type, "Name")
                //    || TryFindProperty(value, type, "name")
                //    || TryFindProperty(value, type, "Name");
            }
        }

        private bool TryFindField(object value, Type type, string name)
        {
            if (type.GetField(name) is FieldInfo nameField && nameField.FieldType == typeof(string))
            {
                BaseKey += nameField.GetValue(value).ToString();
                return true;
            }
            return false;
        }

        private bool TryFindProperty(object value, Type type, string name)
        {
            if (type.GetProperty(name) is PropertyInfo nameField && nameField.PropertyType == typeof(string))
            {
                BaseKey += nameField.GetValue(value).ToString();
                return true;
            }
            return false;
        }
    }
}