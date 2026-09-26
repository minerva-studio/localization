namespace Minerva.Localizations.EscapePatterns
{
    internal sealed class L10nExpression
    {
        public readonly L10nOp[] Ops;
        public readonly float[] Numbers;
        public readonly L10nPath[] Paths;
        public readonly int MaxStack;
        public readonly string Source;
        public readonly string Error;

        public L10nExpression(L10nOp[] ops, float[] numbers, L10nPath[] paths, int maxStack, string source, string error)
        {
            Ops = ops;
            Numbers = numbers;
            Paths = paths;
            MaxStack = maxStack;
            Source = source;
            Error = error;
        }
    }
}
