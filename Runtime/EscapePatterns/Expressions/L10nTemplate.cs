using System;
using System.Collections.Generic;

namespace Minerva.Localizations.EscapePatterns
{
    internal sealed class L10nTemplate
    {
        public enum OpCode : byte { Literal, KeyReference, Expression, ColorOpen, ColorClose }

        internal readonly struct Op
        {
            public readonly OpCode Code;
            public readonly string Text;
            public readonly bool Flag;
            public readonly L10nExpression Expression;
            public readonly string Format;

            public Op(OpCode code, string text = null, bool flag = false, L10nExpression expression = null, string format = null)
            {
                Code = code;
                Text = text;
                Flag = flag;
                Expression = expression;
                Format = format;
            }
        }

        public readonly Op[] Ops;

        private L10nTemplate(Op[] ops) => Ops = ops;

        public static L10nTemplate Compile(string source)
        {
            var tokenizer = L10nObjectPool.RentTokenizer(source.AsMemory());
            L10nToken root = null;
            try
            {
                root = tokenizer.Tokenize();
                var ops = new List<Op>();
                if (root.Children != null)
                    foreach (var token in root.Children) AppendToken(token, ops);
                return new L10nTemplate(ops.ToArray());
            }
            finally
            {
                if (root != null) L10nObjectPool.ReturnToken(root);
                L10nObjectPool.ReturnTokenizer(tokenizer);
            }
        }

        private static void AppendToken(L10nToken token, List<Op> ops)
        {
            switch (token.Type)
            {
                case TokenType.Literal:
                    if (token.Content.Length > 0) ops.Add(new Op(OpCode.Literal, token.Content.ToString()));
                    break;
                case TokenType.KeyReference:
                    ops.Add(new Op(OpCode.KeyReference, token.Content.ToString(), token.IsTooltip));
                    break;
                case TokenType.DynamicValue:
                    ops.Add(new Op(OpCode.Expression, expression: L10nExpressionCompiler.Compile(token.Content.ToString()), format: token.Metadata.ToString()));
                    break;
                case TokenType.ColorTag:
                    ops.Add(new Op(OpCode.ColorOpen, token.Metadata.ToString()));
                    if (token.Children != null)
                    {
                        foreach (var child in token.Children) AppendToken(child, ops);
                    }
                    else if (token.Content.Length > 0)
                    {
                        ops.Add(new Op(OpCode.Literal, token.Content.ToString()));
                    }
                    ops.Add(new Op(OpCode.ColorClose));
                    break;
            }
        }
    }
}
