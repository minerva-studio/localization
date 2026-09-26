using Minerva.Localizations.Utilities;
using System;

namespace Minerva.Localizations
{
    /// <summary>
    /// Common interface use to get the localization information from an object
    /// </summary>
    public interface ILocalizableContext
    {
        /// <summary>
        /// Get the base localization key of the object
        /// </summary>
        virtual string BaseKeyString => GetType().FullName;

        /// <summary>
        /// Get the key represent for this localizable object
        /// </summary>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        virtual string GetLocalizationKey(L10nParams parameters)
        {
            return Localizable.AppendKey(BaseKeyString, parameters.Options);
        }

        /// <summary>
        /// Get the raw content, override this for creating custom format of localized content
        /// </summary>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        virtual string GetRawContent(L10nParams parameters)
        {
            var key = GetLocalizationKey(parameters);
            var rawString = L10n.GetRawContent(key);
            return rawString;
        }

        /// <summary>
        /// Get the raw content but with different key
        /// </summary>
        /// <param name="key">Override key</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        string GetRawContentWithKey(string key, L10nParams parameters)
        {
            var fullKey = Localizable.AppendKey(key, parameters.Options);
            var rawString = L10n.GetRawContent(fullKey);
            return rawString;
        }

        /// <summary>Tries to resolve an escape key to its raw value.</summary>
        /// <param name="escapeKey">The complete canonical escape key.</param>
        /// <param name="parameters">Lookup parameters, including variables and options.</param>
        /// <param name="value">The raw value when found; otherwise null.</param>
        /// <returns>Whether this context resolved the key.</returns>
        virtual bool TryGetEscapeValue(string escapeKey, L10nParams parameters, out object value)
        {
            value = Reflection.TryGetObject(this, escapeKey, out var resolved) ? resolved : null;
            return value != null;
        }
    }

    /// <summary>Provides the legacy display-value behavior for escape lookups.</summary>
    public static class LocalizableContextExtensions
    {
        /// <summary>Gets a localized escape value, returning the key when the context cannot resolve it.</summary>
        public static object GetEscapeValue(this ILocalizableContext context, string escapeKey, L10nParams parameters)
        {
            if (context != null && context.TryGetEscapeValue(escapeKey, parameters, out var value))
                return L10nContext.DynamicValueOf(value, parameters);
            return escapeKey;
        }
    }
}
