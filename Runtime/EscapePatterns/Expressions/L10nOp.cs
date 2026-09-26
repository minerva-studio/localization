namespace Minerva.Localizations.EscapePatterns
{
    internal readonly struct L10nOp
    {
        public enum OpCode : byte { PushNumber, LoadPath, Add, Subtract, Multiply, Divide, Power, Negate }

        public readonly OpCode Code;
        public readonly int Operand;

        public L10nOp(OpCode code, int operand = 0)
        {
            Code = code;
            Operand = operand;
        }
    }
}
