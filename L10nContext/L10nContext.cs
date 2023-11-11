using Minerva.Localizations.EscapePatterns;
using System;
using System.Collections.Generic;

namespace Minerva.Localizations
{
    /// <summary>
    /// Base class of the context of localization
    /// </summary>
    public abstract class L10nContext : ILocalizable, ILocalizer
    {
        private readonly object value;
        protected string baseKey;

        /// <summary>
        /// Get the base localization key of the object
        /// </summary>
        public virtual string BaseKey => baseKey;


        protected L10nContext(object value)
        {
            this.value = value;
            this.baseKey = value?.GetType().FullName ?? string.Empty;
        }


        /// <summary>
        /// Get the key represent for this localizable object
        /// <br></br>
        /// Override to get keys from the object
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual string GetLocalizationKey(params string[] param)
        {
            return Localizable.AppendKey(BaseKey, param);
        }

        /// <summary>
        /// Get the raw content, override this for creating custom format of localized content
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual string GetRawContent(params string[] param)
        {
            var key = GetLocalizationKey(param);
            var rawString = L10n.GetRawContent(key);
            return rawString;
        }

        /// <summary>
        /// Get escape value from the object
        /// </summary>
        /// <param name="escapeKey"></param>
        /// <returns></returns>
        public virtual string GetEscapeValue(string escapeKey, params string[] param)
        {
            var value = GetObjectNullPropagation(this.value, escapeKey);
            if (value == null) return escapeKey;
            return LocalizationOf(value, param);
        }

        /// <summary>
        /// <inheritdoc/> 
        /// <br/>
        /// Short hand for <see cref="L10n.Tr(ILocalizable, string[])"/>
        /// </summary>
        /// <returns>
        /// <inheritdoc/>
        /// </returns>
        public string Tr(params string[] param)
        {
            return Localizable.Tr(this, param);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <br/>
        /// Short hand for <see cref="L10n.TrKey(string, ILocalizable, string[])"/>
        /// </summary>
        /// <returns>
        /// <inheritdoc/>
        /// </returns>
        public string TrKey(string overrideKey, params string[] param)
        {
            return Localizable.TrKey(overrideKey, this, param);
        }



        /// <summary>
        /// Get all possible option of this content
        /// </summary>
        /// <param name="firstLevelOnly"></param>
        /// <returns></returns>
        public List<string> GetOptions(bool firstLevelOnly = false)
        {
            return L10n.OptionOf(baseKey, firstLevelOnly);
        }




        public static string AsKeyEscape(string baseKey, params string[] args)
        {
            return EscapePattern.AsKeyEscape(baseKey, args);
        }

        /// <summary>
        /// Try get the localization of given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string LocalizationOf(object value, params string[] param)
        {
            return Localizable.Tr(value, param);
        }

        /// <summary>
        /// Split action parameter
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static IEnumerable<string[]> GetOpt(params string[] param)
        {
            foreach (var item in param)
            {
                int index = item.IndexOf("=");
                if (index != -1)
                {
                    yield return new string[] { item[..index], item[(index + 1)..] };
                }
                else yield return new string[] { item, null };
            }
        }





        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected static object GetObjectNullPropagation(object obj, string path)
        {
            return Reflection.GetObjectNullPropagation(obj, path);
        }

        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected static object GetObject(object obj, string path)
        {
            return Reflection.GetObject(obj, path);
        }

        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected static string Tr(string key, params string[] param)
        {
            return L10n.Tr(key, param);
        }

        /// <summary>
        /// Create a L10n context
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static L10nContext Of(object value)
        {
            if (value == null)
            {
                return new NoContext();
            }
            Type valueType = value.GetType();
            return (L10nContext)Activator.CreateInstance(CustomContextAttribute.GetContextType(valueType), value);
        }

        /// <summary>
        /// Create a L10n context
        /// </summary>
        /// <remarks>
        /// cannot create a context object for a context object, it will always returns a dynamic context
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static L10nContext Of<T>(T value)
        {
            if (typeof(T) == typeof(DynamicContext)) return ((DynamicContext)(object)value).Clone();
            if (typeof(T).IsSubclassOf(typeof(L10nContext))) return new DynamicContext(value);

            return (L10nContext)Activator.CreateInstance(CustomContextAttribute.GetContextType(typeof(T)), value);
        }

        /// <summary>
        /// Create a L10n context
        /// </summary>
        /// <param name="baseKey"></param>
        /// <returns></returns>
        public static L10nContext Of(string baseKey, params string[] args)
        {
            return new KeyL10nContext(Localizable.AppendKey(baseKey, args));
        }

        /// <summary>
        /// Create a L10n no-context context
        /// </summary>
        /// <returns></returns>
        public static L10nContext None()
        {
            return new NoContext();
        }

        private class NoContext : L10nContext
        {
            public NoContext() : base(null)
            {
            }
        }
    }
}