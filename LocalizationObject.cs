namespace Amlos.Localizations
{
    /// <summary>
    /// Common localization object base class
    /// </summary>
    public abstract class LocalizationObject : ILocalizable
    {
        /// <summary>
        /// Get the base localization key of the object
        /// </summary>
        public virtual string LocalizationBaseKey => GetType().FullName;

        /// <summary>
        /// Get the key represent for this localizable object
        /// <br></br>
        /// Override to get keys from the object
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual string GetLocalizationKey(params string[] param)
        {
            string key = LocalizationBaseKey;
            return Localizable.AppendKey(key, param);
        }

        /// <summary>
        /// Get the raw content, override this for creating custom format of localized content
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual string GetRawContent(params string[] param)
        {
            var key = GetLocalizationKey(param);
            var rawString = Localization.GetContent(key, param);
            return rawString;
        }

        /// <summary>
        /// Get escape value from the object
        /// </summary>
        /// <param name="escapeKey"></param>
        /// <returns></returns>
        public virtual string GetEscapeValue(string escapeKey, params string[] param)
        {
            var value = Reflection.GetObjectNullPropagation(this, escapeKey);
            if (value == null) return escapeKey;
            return value.ToString();
        }
    }
}