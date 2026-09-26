namespace Minerva.Localizations.EscapePatterns
{
    internal readonly struct EvaluationContext
    {
        public int Depth { get; }
        public ILocalizableContext Context { get; }
        public L10nParams Parameters { get; }
        public L10nParams LookupParameters { get; }

        public EvaluationContext(ILocalizableContext context, L10nParams parameters)
            : this(parameters.Depth, context, parameters, parameters.VariablesOnly()) { }

        private EvaluationContext(int depth, ILocalizableContext context, L10nParams parameters, L10nParams lookupParameters)
        {
            Depth = depth;
            Context = context;
            Parameters = parameters;
            LookupParameters = lookupParameters;
        }

        public bool CanRecurse() => Depth < L10n.MAX_RECURSION;

        public EvaluationContext IncreaseDepth() => new(Depth + 1, Context, Parameters.WithDepth(Depth + 1), LookupParameters);
    }
}
