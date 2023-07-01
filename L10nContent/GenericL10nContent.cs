namespace Minerva.Localizations
{
    /// <summary>
    /// Default implementation of localization object
    /// </summary>
    [CustomContent(typeof(object))]
    public sealed class GenericL10nContent : L10nContent
    {
        public GenericL10nContent(object obj) : base(obj) { }
    }
}