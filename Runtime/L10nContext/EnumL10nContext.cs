using System;
using System.Collections.Generic;
using System.Linq;

namespace Minerva.Localizations
{
    /// <summary>
    /// L10n content directly use given string as base key
    /// </summary>
    public class EnumL10nContext : L10nContext
    {
        private string path;
        private bool isL10nFlagEnum;

        public EnumL10nContext() { }

        public EnumL10nContext(Enum value) : base(value) { }

        protected override void Parse(object value)
        {
            path = L10nAlias.GetTypeName(value.GetType());
            isL10nFlagEnum = Attribute.GetCustomAttribute(BaseValue.GetType(), typeof(L10nFlagEnumAttribute)) is L10nFlagEnumAttribute;

            if (isL10nFlagEnum)
                BaseKey = new Key(path);
            else
                BaseKey = new Key(path, value.ToString());
        }

        public override string GetRawContent(L10nParams parameters)
        {
            if (isL10nFlagEnum)
            {
                var flags = FlagEnumSplit(BaseValue.GetType(), BaseValue as Enum);
                return string.Join(L10n.ListDelimiter, flags.Select(s => $"${Localizable.AppendKey($"{path}.{s}", parameters.Options)}$"));
            }
            else return base.GetRawContent(parameters);
        }

        public IEnumerable<Enum> FlagEnumSplit(Type type, Enum e)
        {
            int value = 1;
            for (int i = 0; i < 32; i++)
            {
                if (Enum.IsDefined(type, value))
                {
                    Enum positionEnum = Enum.Parse(type, value.ToString()) as Enum;
                    if (e.HasFlag(positionEnum))
                        yield return positionEnum;
                }
                value <<= 1;
            }
        }
    }
}
