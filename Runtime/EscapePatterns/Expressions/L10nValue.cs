using System;
using System.Globalization;

namespace Minerva.Localizations.EscapePatterns
{
    internal readonly struct L10nValue
    {
        public enum ValueKind : byte { Number, Object }

        public readonly ValueKind Kind;
        public readonly float Number;
        public readonly object Object;

        private L10nValue(ValueKind kind, float number, object value)
        {
            Kind = kind;
            Number = number;
            Object = value;
        }

        public static L10nValue FromNumber(float value) => new(ValueKind.Number, value, null);
        public static L10nValue FromObject(object value) => new(ValueKind.Object, 0, value);

        public bool TryGetNumber(out float value)
        {
            if (Kind == ValueKind.Number)
            {
                value = Number;
                return true;
            }

            switch (Object)
            {
                case float f: value = f; return true;
                case double d: value = (float)d; return true;
                case int i: value = i; return true;
                case long l: value = l; return true;
                case string text: return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                default: value = 0; return false;
            }
        }

        public object ToObject() => Kind == ValueKind.Number ? Number : Object;
    }
}
