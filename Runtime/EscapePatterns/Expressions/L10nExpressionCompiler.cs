using System;
using System.Collections.Generic;
using System.Globalization;

namespace Minerva.Localizations.EscapePatterns
{
    internal static class L10nExpressionCompiler
    {
        public static L10nExpression Compile(string source)
        {
            source ??= string.Empty;
            var builder = new Builder();
            var parser = new Parser(source.AsSpan(), builder);
            try
            {
                parser.ParseExpression(1);
                parser.SkipWhitespace();
                if (!parser.AtEnd) parser.Fail("Unexpected token");
                return builder.Build(source);
            }
            catch (CompileException e)
            {
                return new L10nExpression(Array.Empty<L10nOp>(), Array.Empty<double>(), Array.Empty<L10nPath>(), 0, source, e.Message);
            }
        }

        private sealed class Builder
        {
            public readonly List<L10nOp> Ops = new();
            public readonly List<double> Numbers = new();
            public readonly List<L10nPath> Paths = new();

            public int AddNumber(double value)
            {
                Numbers.Add(value);
                return Numbers.Count - 1;
            }

            public int AddPath(L10nPath path)
            {
                Paths.Add(path);
                return Paths.Count - 1;
            }

            public L10nExpression Build(string source) => new(Ops.ToArray(), Numbers.ToArray(), Paths.ToArray(), GetMaxStack(), source, null);

            public int GetMaxStack()
            {
                int depth = 0;
                int max = 0;
                foreach (var op in Ops)
                {
                    if (op.Code is L10nOp.OpCode.PushNumber or L10nOp.OpCode.LoadPath) depth++;
                    else if (op.Code is L10nOp.OpCode.Add or L10nOp.OpCode.Subtract or L10nOp.OpCode.Multiply or L10nOp.OpCode.Divide or L10nOp.OpCode.Power) depth--;
                    if (depth > max) max = depth;
                }
                return max;
            }
        }

        private sealed class CompileException : Exception
        {
            public CompileException(string message) : base(message) { }
        }

        private ref struct Parser
        {
            private readonly ReadOnlySpan<char> source;
            private readonly Builder builder;
            private int position;

            public Parser(ReadOnlySpan<char> source, Builder builder)
            {
                this.source = source;
                this.builder = builder;
                position = 0;
            }

            public bool AtEnd => position >= source.Length;

            public void ParseExpression(int minPrecedence)
            {
                ParseUnary();
                while (true)
                {
                    SkipWhitespace();
                    var code = CurrentBinaryOp(out int precedence, out bool rightAssociative);
                    if (precedence < minPrecedence) return;
                    position++;
                    ParseExpression(rightAssociative ? precedence : precedence + 1);
                    builder.Ops.Add(new L10nOp(code));
                }
            }

            private void ParseUnary()
            {
                SkipWhitespace();
                if (TryConsume('+'))
                {
                    ParseUnary();
                    return;
                }
                if (TryConsume('-'))
                {
                    ParseUnary();
                    builder.Ops.Add(new L10nOp(L10nOp.OpCode.Negate));
                    return;
                }
                ParsePrimary();
            }

            private void ParsePrimary()
            {
                SkipWhitespace();
                if (TryConsume('('))
                {
                    ParseExpression(1);
                    SkipWhitespace();
                    if (!TryConsume(')')) Fail("Missing closing parenthesis");
                    return;
                }

                if (AtEnd) Fail("Expected a number, path, or parenthesized expression");
                if (char.IsDigit(source[position]) || source[position] == '.' && position + 1 < source.Length && char.IsDigit(source[position + 1]))
                {
                    ParseNumber();
                    return;
                }
                if (IsIdentifierStart(source[position]))
                {
                    ParsePath();
                    return;
                }
                Fail("Expected a number, path, or parenthesized expression");
            }

            private void ParseNumber()
            {
                int start = position;
                bool sawDot = false;
                int digitCount = 0;
                int fractionalDigits = 0;
                bool afterDot = false;
                if (source[position] == '.')
                {
                    sawDot = true;
                    afterDot = true;
                    position++;
                }
                while (position < source.Length)
                {
                    char c = source[position];
                    if (char.IsDigit(c)) { digitCount++; if (afterDot) fractionalDigits++; position++; continue; }
                    if (c == '.' && !sawDot) { sawDot = true; afterDot = true; position++; continue; }
                    break;
                }
                if (digitCount == 0 || sawDot && fractionalDigits == 0) FailAt(start, "Invalid numeric literal");
                if (!double.TryParse(source.Slice(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    FailAt(start, "Invalid numeric literal");
                int operand = builder.AddNumber(value);
                builder.Ops.Add(new L10nOp(L10nOp.OpCode.PushNumber, operand));
            }

            private void ParsePath()
            {
                var segments = new List<L10nPath.Segment>();
                segments.Add(L10nPath.Segment.Member(ReadIdentifier()));
                while (true)
                {
                    SkipWhitespace();
                    if (TryConsume('.'))
                    {
                        SkipWhitespace();
                        if (AtEnd || !IsIdentifierStart(source[position])) Fail("Expected member name after '.'");
                        segments.Add(L10nPath.Segment.Member(ReadIdentifier()));
                        continue;
                    }
                    if (TryConsume('['))
                    {
                        int expressionStart = position;
                        var nestedBuilder = new Builder();
                        var nestedParser = new Parser(source.Slice(position), nestedBuilder);
                        nestedParser.ParseExpression(1);
                        nestedParser.SkipWhitespace();
                        position += nestedParser.position;
                        int expressionEnd = position;
                        if (!TryConsume(']')) Fail("Missing closing index bracket");
                        var indexExpression = nestedBuilder.Build(source.Slice(expressionStart, expressionEnd - expressionStart).ToString());
                        if (IsSingleNumberLiteral(indexExpression, out double number))
                        {
                            if (double.IsNaN(number) || double.IsInfinity(number) || number < int.MinValue || number > int.MaxValue || Math.Truncate(number) != number)
                                FailAt(expressionStart, "Index literal must be a finite Int32 value");
                            segments.Add(L10nPath.Segment.LiteralIndex((int)number));
                        }
                        else segments.Add(L10nPath.Segment.DynamicIndex(indexExpression));
                        continue;
                    }
                    break;
                }

                L10nParams? args = null;
                SkipWhitespace();
                if (TryConsume('<'))
                {
                    int start = position;
                    while (!AtEnd && source[position] != '>') position++;
                    if (!TryConsume('>')) Fail("Missing closing '>' for path arguments");
                    args = L10nParams.ParseParameters(source.Slice(start, position - start - 1).ToString().AsMemory());
                    SkipWhitespace();
                    if (!AtEnd && (source[position] == '.' || source[position] == '[')) Fail("Path arguments must be at the end of a path");
                }

                int pathIndex = builder.AddPath(new L10nPath(segments.ToArray(), args));
                builder.Ops.Add(new L10nOp(L10nOp.OpCode.LoadPath, pathIndex));
            }

            private static bool IsSingleNumberLiteral(L10nExpression expression, out double value)
            {
                value = 0;
                if (expression.Ops.Length != 1 || expression.Ops[0].Code != L10nOp.OpCode.PushNumber) return false;
                value = expression.Numbers[expression.Ops[0].Operand];
                return true;
            }

            private string ReadIdentifier()
            {
                int start = position++;
                while (position < source.Length && IsIdentifierPart(source[position])) position++;
                return source.Slice(start, position - start).ToString();
            }

            private L10nOp.OpCode CurrentBinaryOp(out int precedence, out bool rightAssociative)
            {
                rightAssociative = false;
                if (AtEnd) { precedence = 0; return default; }
                switch (source[position])
                {
                    case '^': precedence = 4; rightAssociative = true; return L10nOp.OpCode.Power;
                    case '*': precedence = 3; return L10nOp.OpCode.Multiply;
                    case '/': precedence = 3; return L10nOp.OpCode.Divide;
                    case '+': precedence = 2; return L10nOp.OpCode.Add;
                    case '-': precedence = 2; return L10nOp.OpCode.Subtract;
                    default: precedence = 0; return default;
                }
            }

            public void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(source[position])) position++;
            }

            private bool TryConsume(char c)
            {
                if (!AtEnd && source[position] == c) { position++; return true; }
                return false;
            }

            public void Fail(string message) => FailAt(position, message);
            private static bool IsIdentifierStart(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
            private static bool IsIdentifierPart(char c) => IsIdentifierStart(c) || c is >= '0' and <= '9';

            private void FailAt(int at, string message)
            {
                throw new CompileException($"{message} at position {at}.");
            }
        }
    }
}
