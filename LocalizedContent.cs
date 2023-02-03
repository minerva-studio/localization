using Amlos.Localizations.EscapePatterns;
using System;

namespace Amlos.Localizations
{
    /// <summary>
    /// A packed localized content
    /// </summary>
    public abstract class LocalizedContent : ILocalizer
    {
        public LocalizedContent() { }

        /// <summary>
        /// Get localized content with extra parameters
        /// </summary>
        /// <param name="param">extra parameters</param>
        /// <returns>localized content</returns>
        public abstract string Localize(params string[] param);




        /// <summary>
        /// create a localized content
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static LocalizedContent Create(object obj)
        {
            return obj is ILocalizable localizable ? Create(localizable) : new ObjectContent(obj);
        }

        /// <summary>
        /// create a localized content
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static LocalizedContent Create(ILocalizable obj)
        {
            return new LocalizableObjectContent(obj);
        }

        /// <summary>
        /// create a localized content by specifying the key directly
        /// </summary>
        /// <param name="key">the key to create</param>
        /// <returns></returns>
        public static LocalizedContent Create(string key)
        {
            return new KeyLocalizedContent(key);
        }




        /// <summary>
        /// Localized Content with constant key value
        /// </summary>
        class KeyLocalizedContent : LocalizedContent, ILocalizer
        {
            readonly string key;

            public KeyLocalizedContent(string key)
            {
                this.key = key ?? throw new ArgumentNullException(nameof(key));
            }

            /// <summary>
            /// Get localized content with extra parameters
            /// </summary>
            /// <param name="param">extra parameters</param>
            /// <returns>localized content</returns>
            public override string Localize(params string[] param)
            {
                return Localization.GetContent(Localizable.AppendKey(key, param), param);
            }


            public override string ToString()
            {
                return key;
            }
        }

        /// <summary>
        /// Object Localized content with an object as key
        /// </summary>
        class ObjectContent : LocalizedContent, ILocalizer
        {
            readonly object obj;


            public ObjectContent(object obj)
            {
                this.obj = obj ?? throw new ArgumentNullException(nameof(obj));
            }

            /// <summary>
            /// Get localized content with extra parameters
            /// </summary>
            /// <param name="param">extra parameters</param>
            /// <returns>localized content</returns>
            public override string Localize(params string[] param)
            {
                string fullName = obj.GetType().FullName;
                var rawString = Localization.GetContent(fullName, param);
                return rawString;
            }

            public override string ToString()
            {
                return obj.GetType().FullName;
            }
        }

        /// <summary>
        /// Object Localized content with an object as key
        /// </summary>
        class LocalizableObjectContent : LocalizedContent, ILocalizer
        {
            readonly ILocalizable obj;

            public LocalizableObjectContent(ILocalizable obj)
            {
                this.obj = obj ?? throw new ArgumentNullException(nameof(obj));
            }

            /// <summary>
            /// Get localized content with extra parameters
            /// </summary>
            /// <param name="param">extra parameters</param>
            /// <returns>localized content</returns>
            public override string Localize(params string[] param)
            {
                var rawString = obj.GetRawContent(param);
                rawString = EscapePattern.ReplaceKeyEscape(rawString);
                rawString = EscapePattern.ReplaceColorEscape(rawString);
                rawString = EscapePattern.ReplaceDynamicValueEscape(rawString, obj, param);
                return rawString;
            }


            public override string ToString()
            {
                return obj.GetLocalizationKey();
            }
        }
    }
}