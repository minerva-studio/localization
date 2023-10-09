namespace Minerva.Localizations
{
    /// <summary>
    /// Default implementation of localization object
    /// </summary>
    [CustomContent(typeof(object))]
    public sealed class GenericL10nContext : L10nContext
    {
        public GenericL10nContext(object obj) : base(obj) { }
    }
}