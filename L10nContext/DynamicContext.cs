using Minerva.Localizations.EscapePatterns;
using System.Collections.Generic;

namespace Minerva.Localizations
{
    /// <summary>
    /// A Context type allowing changing dynamic value
    /// </summary> 
    public class DynamicContext : L10nContext
    {
        readonly Dictionary<string, string> dynamicValues = new();
        readonly L10nContext parentContext;

        public string this[string index]
        {
            get => dynamicValues[index];
            set => dynamicValues[index] = value;
        }

        public DynamicContext(object value) : base(value)
        {
            if (value is L10nContext context)
            {
                parentContext = context;
            }
        }

        public DynamicContext(object value, Dictionary<string, string> dynamicValues) : this(value)
        {
            this.dynamicValues = dynamicValues;
        }


        public bool IsDefined(string key)
        {
            return dynamicValues.ContainsKey(key);
        }

        public void Parse(params string[] param)
        {
            EscapePattern.ParseDynamicValue(dynamicValues, true, param);
        }

        public override string GetEscapeValue(string escapeKey, params string[] param)
        {
            if (IsDefined(escapeKey)) return this[escapeKey];
            if (parentContext != null) return parentContext.GetEscapeValue(escapeKey, param);
            return base.GetEscapeValue(escapeKey, param);
        }

        public L10nContext Clone()
        {
            return new DynamicContext(parentContext, new Dictionary<string, string>(dynamicValues));
        }
    }
}