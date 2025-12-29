namespace Minerva.Localizations
{
    /// <summary>
    /// A context without object but a direct raw context with it.
    /// </summary>
    public class RawContentL10nContext : L10nContext
    {
        string rawContent;

        public string RawContent { get => rawContent; set => rawContent = value; }

        public RawContentL10nContext()
        {
            BaseValue = this;
        }

        protected override void Parse(object value)
        {
            if (value is string str)
            {
                RawContent = str;
            }
            else RawContent = value.ToString();
        }

        public override string GetRawContent(L10nParams param)
        {
            return RawContent;
        }
    }
}
