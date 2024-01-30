namespace Minerva.Localizations
{
    /// <summary>
    /// Common interface use to get the localization information from an object
    /// </summary>
    public interface ILocalizable
    {
        /// <summary>
        /// Get the base localization key of the object
        /// </summary>
        virtual string BaseKey => GetType().FullName;

        /// <summary>
        /// Get the key represent for this localizable object
        /// <br></br>
        /// Override to get keys from the object
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        virtual string GetLocalizationKey(params string[] param)
        {
            return Localizable.AppendKey(BaseKey, param);
        }

        /// <summary>
        /// Get the raw content, override this for creating custom format of localized content
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        virtual string GetRawContent(params string[] param)
        {
            var key = GetLocalizationKey(param);
            var rawString = L10n.GetRawContent(key);
            return rawString;
        }

        /// <summary>
        /// Get the raw content but with different key, override this for creating custom format of localized content
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        string GetRawContentWithKey(string key, params string[] param)
        {
            var rawString = L10n.GetRawContent(key);
            return rawString;
        }

        /// <summary>
        /// Get escape value from the object
        /// </summary>
        /// <param name="escapeKey"></param>
        /// <returns></returns>
        virtual string GetEscapeValue(string escapeKey, params string[] param)
        {
            var value = Reflection.GetObjectNullPropagation(this, escapeKey);
            if (value == null) return escapeKey;
            return Localizable.Tr(value, param);
        }
    }
}