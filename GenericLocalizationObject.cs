namespace Minerva.Localizations
{
    /// <summary>
    /// Default implementation of localization object
    /// </summary>
    public sealed class GenericLocalizationObject : LocalizationObject
    {
        public GenericLocalizationObject(object obj) : base(obj?.GetType().FullName)
        {
        }
    }
}