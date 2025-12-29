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
            var rawString = L10n.GetRawContent(key);
            return rawString;
        }

        /// <summary>
        /// Get escape value from the object
        /// </summary>
        /// <param name="escapeKey">The escape key</param>
        /// <param name="parameters">Localization parameters</param>
        /// <returns></returns>
        virtual object GetEscapeValue(string escapeKey, L10nParams parameters)
        {
            var value = Reflection.GetObjectNullPropagation(this, escapeKey.AsMemory());
            if (value == null) return escapeKey;
            return value;
        }
    }
}