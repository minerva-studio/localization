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
        string path;

        public EnumL10nContext() { }

        public EnumL10nContext(Enum value) : base(value) { }

        protected override void Parse(object value)
        {
            path = L10nAlias.GetTypeName(value.GetType());
            BaseKey = new Key(path, value.ToString());
        }

        public override string GetRawContent(params string[] param)
        {
            if (Attribute.GetCustomAttribute(BaseValue.GetType(), typeof(L10nFlagEnumAttribute)) is L10nFlagEnumAttribute)
            {
                var flags = FlagEnumSplit(BaseValue.GetType(), BaseValue as Enum);
                return string.Join(L10n.ListDelimiter, flags.Select(s => Localizable.AppendKey($"{path}.{s}", param)));
            }
            else return base.GetRawContent(param);
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