namespace Minerva.Localizations
{
    /// <summary>
    /// A localizer, provide basic functions of how to translate given options to translated text
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Get localized content with extra parameters
        /// </summary>
        /// <param name="param">extra parameters</param>
        /// <returns>localized content</returns>
        string Tr(params string[] param);

        /// <summary>
        /// override the key and get localized content with extra parameters
        /// </summary>
        /// <param name="param">extra parameters</param>
        /// <returns>localized content</returns>
        string TrKey(string key, params string[] param);
    }
}