using System.Collections.Generic;

namespace Minerva.Localizations.EscapePatterns
{
    internal sealed class EvaluationContext
    {
        public int Depth { get; }
        public ILocalizableContext Context { get; }
        public Dictionary<string, object> Variables { get; }

        public EvaluationContext(int depth, ILocalizableContext context, Dictionary<string, object> variables)
        {
            Depth = depth;
            Context = context;
            Variables = variables ?? new Dictionary<string, object>();
        }

        public bool CanRecurse() => Depth < L10n.MAX_RECURSION;

        public EvaluationContext IncreaseDepth()
        {
            return new EvaluationContext(Depth + 1, Context, Variables);
        }
    }
}