namespace Minerva.Localizations
{
    /// <summary>
    /// A localizer
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Get localized content with extra parameters
        /// </summary>
        /// <param name="param">extra parameters</param>
        /// <returns>localized content</returns>
        string Localize(params string[] param);
    }
}