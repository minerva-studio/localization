using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Minerva.Localizations.EscapePatterns.Regexes;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Parser of the dynamic value/expression in localization system
    /// </summary>
    public class ExpressionParser
    {
        public delegate object VariableValueProvider(ReadOnlyMemory<char> expr);

        // Token types
        public enum TokenType
        {
            Number,
            Variable,
            Plus,
            Minus,
            Multiply,
            Divide,
            Power,
            LeftParen,
            RightParen,
            LeftBracket,
            RightBracket,
            Dot,
            End
        }

        // Token structure
        public class Token
        {
            public TokenType Type { get; }
            public ReadOnlyMemory<char> Value { get; }
            public int Position { get; }

            public Token(TokenType type, ReadOnlyMemory<char> value, int position)
            {
                Type = type;
                Value = value;
                Position = position;
            }

            public override string ToString()
            {
                return $"{Type} ('{Value}') at Position {Position}";
            }
        }

        // Lexer to tokenize the input string
        public class Lexer
        {
            private readonly ReadOnlyMemory<char> _input;
            private int _position;
            private readonly L10nEvaluationDiagnostics _diagnostics;
            private readonly string _expressionContext;

            public Lexer(string input, L10nEvaluationDiagnostics diagnostics = null, string expressionContext = null)
            {
                _input = input.AsMemory();
                _diagnostics = diagnostics;
                _expressionContext = expressionContext ?? input;
            }

            public Lexer(ReadOnlyMemory<char> input, L10nEvaluationDiagnostics diagnostics = null, string expressionContext = null)
            {
                _input = input;
                _diagnostics = diagnostics;
                _expressionContext = expressionContext ?? input.ToString();
            }

            public Token GetNextToken()
            {
                SkipWhitespace();
                if (_position >= _input.Length)
                    return new Token(TokenType.End, ReadOnlyMemory<char>.Empty, _position);

                char current = _input.Span[_position];
                // start of number: digit, '.', or a '-' followed by digit/'.'
                if (IsNumer(current))
                    return NumberToken();

                if (current == '.' && (_position + 1 >= _input.Length || !char.IsDigit(_input.Span[_position + 1])))
                    return CreateToken(TokenType.Dot, ".");
                if (IsValidTokenInVariable(current))
                    return VariableToken();
                if (current == '+')
                    return CreateToken(TokenType.Plus, "+");
                if (current == '-')
                    return CreateToken(TokenType.Minus, "-");
                if (current == '*')
                    return CreateToken(TokenType.Multiply, "*");
                if (current == '^')
                    return CreateToken(TokenType.Power, "^");
                if (current == '/')
                    return CreateToken(TokenType.Divide, "/");
                if (current == '(')
                    return CreateToken(TokenType.LeftParen, "(");
                if (current == ')')
                    return CreateToken(TokenType.RightParen, ")");
                if (current == '[')
                    return CreateToken(TokenType.LeftBracket, "[");
                if (current == ']')
                    return CreateToken(TokenType.RightBracket, "]");

                var ex = new Exception($"Unexpected character '{current}' at position {_position}");
                _diagnostics?.AddError(
                    L10nErrorSeverity.Error,
                    _expressionContext,
                    "LexerError",
                    ex.Message,
                    ex);
                throw ex;
            }

            private bool IsNumer(char current)
            {
                // plain number or "."
                if (char.IsDigit(current) || (current == '.' && _position + 1 < _input.Length && char.IsDigit(_input.Span[_position + 1]))) return true;

                // possible signed literal: only treat '-' as part of number in *unary* contexts
                if (current == '-')
                {
                    // lookahead: must be followed by a digit or '.'
                    bool nextOk = _position + 1 < _input.Length &&
                                  (char.IsDigit(_input.Span[_position + 1]) || _input.Span[_position + 1] == '.');
                    if (!nextOk) return false;

                    // lookbehind (skip spaces): start-of-input or after an operator or '('
                    int i = _position - 1;
                    while (i >= 0 && char.IsWhiteSpace(_input.Span[i])) i--;
                    if (i < 0) return true; // beginning of expression

                    char prev = _input.Span[i];
                    return prev == '(' || prev == '+' || prev == '-' || prev == '*' || prev == '/' || prev == '^';
                }

                return false;
            }

            private void SkipWhitespace()
            {
                while (_position < _input.Length && char.IsWhiteSpace(_input.Span[_position]))
                    _position++;
            }

            private Token NumberToken()
            {
                int start = _position;
                bool hasDot = false;

                // Optional leading sign support
                if (_position < _input.Length && _input.Span[_position] == '-')
                {
                    // only treat '-' as a sign if followed by a digit or '.'
                    if (_position + 1 < _input.Length && (char.IsDigit(_input.Span[_position + 1]) || _input.Span[_position + 1] == '.'))
                        _position++;
                    else
                    {
                        var ex = new Exception($"Unexpected '-' at position {start}");
                        _diagnostics?.AddError(
                            L10nErrorSeverity.Error,
                            _expressionContext,
                            "NumberFormatError",
                            ex.Message,
                            ex);
                        throw ex;
                    }
                }

                while (_position < _input.Length && (char.IsDigit(_input.Span[_position]) || _input.Span[_position] == '.'))
                {
                    if (_input.Span[_position] == '.')
                    {
                        if (hasDot) // Multiple dots are invalid
                        {
                            var ex = new Exception($"Invalid number format at position {start}");
                            _diagnostics?.AddError(
                                L10nErrorSeverity.Error,
                                _expressionContext,
                                "NumberFormatError",
                                ex.Message,
                                ex);
                            throw ex;
                        }
                        hasDot = true;
                    }
                    _position++;
                }

                return new Token(TokenType.Number, _input[start.._position], start);
            }

            private Token VariableToken()
            {
                int start = _position;

                while (_position < _input.Length && IsValidTokenInVariable(_input.Span[_position]))
                    _position++;

                var span = _input[start.._position];
                // Validate against the dynamic value argument pattern
                if (!DYNAMIC_ARG_PATTERN.IsMatch(span.ToString()))
                {
                    var ex = new Exception($"Invalid variable format '{span}' at position {start}");
                    _diagnostics?.AddError(
                        L10nErrorSeverity.Error,
                        _expressionContext,
                        "InvalidVariableFormat",
                        ex.Message,
                        ex);
                    throw ex;
                }

                return new Token(TokenType.Variable, span, start);
            }

            private bool IsValidTokenInVariable(char c)
            {
                if (char.IsLetterOrDigit(c)) return true;
                switch (c)
                {
                    case '<':
                    case '>':
                    case ',': // variable parameter separator
                    case ':':
                    case '=':
                    case '.':
                    case '~':
                    case '_':
                        return true;
                    default:
                        return false;
                }
            }

            private Token CreateToken(TokenType type, string value)
            {
                _position++;
                return new Token(type, value.AsMemory(), _position - 1);
            }
        }

        // Parser for generating an abstract syntax tree (AST)
        public class Parser
        {
            private readonly Lexer _lexer;
            private Token _currentToken;
            private readonly L10nEvaluationDiagnostics _diagnostics;
            private readonly string _expressionContext;

            public Parser(string input, L10nEvaluationDiagnostics diagnostics = null)
            {
                _diagnostics = diagnostics;
                _expressionContext = input;
                _lexer = new Lexer(input, diagnostics, input);
                _currentToken = _lexer.GetNextToken();
            }

            public Parser(ReadOnlyMemory<char> input, L10nEvaluationDiagnostics diagnostics = null)
            {
                _diagnostics = diagnostics;
                _expressionContext = input.ToString();
                _lexer = new Lexer(input, diagnostics, input.ToString());
                _currentToken = _lexer.GetNextToken();
            }

            public Node ParseExpression()
            {
                Node expression = ParseBinaryExpression(1);
                if (_currentToken.Type != TokenType.End)
                    throw SyntaxError($"Unexpected token {_currentToken} after expression");
                return expression;
            }

            private Node ParseBinaryExpression(int minPrecedence)
            {
                Node left = ParseUnary();

                while (true)
                {
                    int prec = GetPrecedence(_currentToken.Type, out bool rightAssociative);
                    if (prec < minPrecedence) break;

                    Token op = _currentToken;
                    AdvanceToken();

                    int nextMin = rightAssociative ? prec : prec + 1;
                    Node right = ParseBinaryExpression(nextMin);

                    left = new BinaryOperationNode(left, op, right);
                }

                return left;
            }

            private Node ParseUnary()
            {
                if (_currentToken.Type == TokenType.Plus)
                {
                    AdvanceToken();
                    return ParseUnary();
                }
                if (_currentToken.Type == TokenType.Minus)
                {
                    Token minus = _currentToken;
                    AdvanceToken();
                    var negOne = new NumberNode(new Token(TokenType.Number, "-1".AsMemory(), minus.Position));
                    return new BinaryOperationNode(negOne, new Token(TokenType.Multiply, "*".AsMemory(), minus.Position), ParseUnary());
                }
                return ParsePostfix(ParsePrimary());
            }

            private Node ParsePostfix(Node node)
            {
                if (node is not VariableNode variable) return node;
                AccessNode access = new RootAccessNode(variable.Name, variable.Position);
                bool hasPostfix = false;
                while (_currentToken.Type == TokenType.LeftBracket || _currentToken.Type == TokenType.Dot)
                {
                    hasPostfix = true;
                    if (_currentToken.Type == TokenType.LeftBracket)
                    {
                        AdvanceToken();
                        Node index = ParseBinaryExpression(1);
                        if (_currentToken.Type != TokenType.RightBracket)
                            throw SyntaxError($"Missing closing index bracket at position {_currentToken.Position}");
                        AdvanceToken();
                        access = new IndexAccessNode(access, index, access.Position);
                    }
                    else
                    {
                        AdvanceToken();
                        if (_currentToken.Type != TokenType.Variable)
                            throw SyntaxError($"Expected member name at position {_currentToken.Position}");
                        string[] members = _currentToken.Value.ToString().Split('.');
                        if (members.Any(string.IsNullOrEmpty))
                            throw SyntaxError($"Invalid member path at position {_currentToken.Position}");
                        foreach (string member in members)
                            access = new MemberAccessNode(access, member, _currentToken.Position);
                        AdvanceToken();
                    }
                }
                return hasPostfix ? access : node;
            }

            private Node ParsePrimary()
            {
                if (_currentToken.Type == TokenType.Number)
                {
                    Node node = new NumberNode(_currentToken);
                    AdvanceToken();
                    return node;
                }
                else if (_currentToken.Type == TokenType.Variable)
                {
                    Node node = new VariableNode(_currentToken);
                    AdvanceToken();
                    return node;
                }
                else if (_currentToken.Type == TokenType.LeftParen)
                {
                    AdvanceToken();
                    Node node = ParseBinaryExpression(1);
                    if (_currentToken.Type != TokenType.RightParen)
                    {
                        var ex = new Exception($"Missing closing parenthesis at position {_currentToken.Position}");
                        _diagnostics?.AddError(
                            L10nErrorSeverity.Error,
                            _expressionContext,
                            "SyntaxError",
                            ex.Message,
                            ex);
                        throw ex;
                    }
                    AdvanceToken();
                    return node;
                }

                var syntaxEx = new Exception($"Invalid syntax at position {_currentToken.Position} ({_currentToken})");
                _diagnostics?.AddError(
                    L10nErrorSeverity.Error,
                    _expressionContext,
                    "SyntaxError",
                    syntaxEx.Message,
                    syntaxEx);
                throw syntaxEx;
            }

            private void AdvanceToken()
            {
                _currentToken = _lexer.GetNextToken();
            }

            private Exception SyntaxError(string message)
            {
                var ex = new Exception(message);
                _diagnostics?.AddError(L10nErrorSeverity.Error, _expressionContext, "SyntaxError", ex.Message, ex);
                return ex;
            }

            private static int GetPrecedence(TokenType t, out bool rightAssociative)
            {
                rightAssociative = false;
                return t switch
                {
                    TokenType.Power => (rightAssociative = true, 4).Item2,
                    TokenType.Multiply => 3,
                    TokenType.Divide => 3,
                    TokenType.Plus => 2,
                    TokenType.Minus => 2,
                    _ => 0,
                };
            }
        }

        // AST Nodes
        public abstract class Node
        {
            public int Position { get; }

            protected Node(int position)
            {
                Position = position;
            }

            public abstract object Run(VariableValueProvider variableValueProvider);
        }

        public class NumberNode : Node
        {
            public string Value { get; }

            public NumberNode(Token token) : base(token.Position)
            {
                Value = token.Value.ToString();
            }

            public override object Run(VariableValueProvider variableValueProvider)
            {
                return float.Parse(Value, provider: CultureInfo.InvariantCulture);
            }
        }

        public class VariableNode : Node
        {
            public ReadOnlyMemory<char> Name { get; }

            public VariableNode(Token token) : base(token.Position)
            {
                Name = token.Value;
            }

            public override object Run(VariableValueProvider variableValueProvider)
            {
                return variableValueProvider(Name);
            }
        }

        internal abstract class AccessNode : Node
        {
            protected AccessNode(int position) : base(position) { }

            public sealed override object Run(VariableValueProvider variableValueProvider)
            {
                return variableValueProvider(MaterializePath(variableValueProvider).AsMemory());
            }

            internal string MaterializePath(VariableValueProvider resolveIndexSymbol)
            {
                var builder = new System.Text.StringBuilder();
                AppendPath(builder, resolveIndexSymbol);
                return builder.ToString();
            }

            internal abstract void AppendPath(System.Text.StringBuilder builder, VariableValueProvider resolveIndexSymbol);
        }

        private sealed class RootAccessNode : AccessNode
        {
            private readonly ReadOnlyMemory<char> _name;
            public RootAccessNode(ReadOnlyMemory<char> name, int position) : base(position) => _name = name;
            internal override void AppendPath(System.Text.StringBuilder builder, VariableValueProvider resolveIndexSymbol) => builder.Append(_name.Span);
        }

        private sealed class MemberAccessNode : AccessNode
        {
            private readonly AccessNode _target;
            private readonly string _member;
            public MemberAccessNode(AccessNode target, string member, int position) : base(position)
            {
                _target = target;
                _member = member;
            }
            internal override void AppendPath(System.Text.StringBuilder builder, VariableValueProvider resolveIndexSymbol)
            {
                _target.AppendPath(builder, resolveIndexSymbol);
                builder.Append('.').Append(_member);
            }
        }

        private sealed class IndexAccessNode : AccessNode
        {
            private readonly AccessNode _target;
            private readonly Node _index;
            public IndexAccessNode(AccessNode target, Node index, int position) : base(position)
            {
                _target = target;
                _index = index;
            }
            internal override void AppendPath(System.Text.StringBuilder builder, VariableValueProvider resolveIndexSymbol)
            {
                object value = _index.Run(resolveIndexSymbol);
                if (!TryConvertIndex(value, out int index))
                    throw new FormatException($"Index expression at position {Position} must evaluate to an integer.");
                _target.AppendPath(builder, resolveIndexSymbol);
                builder.Append('[').Append(index.ToString(CultureInfo.InvariantCulture)).Append(']');
            }

            private static bool TryConvertIndex(object value, out int index)
            {
                index = 0;
                switch (value)
                {
                    case int i: index = i; return true;
                    case sbyte sb: index = sb; return true;
                    case short s: index = s; return true;
                    case byte b: index = b; return true;
                    case ushort us: index = us; return true;
                    case long l when l >= int.MinValue && l <= int.MaxValue: index = (int)l; return true;
                    case uint ui when ui <= int.MaxValue: index = (int)ui; return true;
                    case ulong ul when ul <= int.MaxValue: index = (int)ul; return true;
                    case float f when !float.IsNaN(f) && !float.IsInfinity(f) && (double)f >= int.MinValue && (double)f <= int.MaxValue && Math.Truncate(f) == f: index = (int)f; return true;
                    case double d when !double.IsNaN(d) && !double.IsInfinity(d) && d >= int.MinValue && d <= int.MaxValue && Math.Truncate(d) == d: index = (int)d; return true;
                    case decimal m when m >= int.MinValue && m <= int.MaxValue && decimal.Truncate(m) == m: index = (int)m; return true;
                    case string text:
                        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) return true;
                        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsed) &&
                            parsed >= int.MinValue && parsed <= int.MaxValue && decimal.Truncate(parsed) == parsed)
                        {
                            index = (int)parsed;
                            return true;
                        }
                        return false;
                    default: return false;
                }
            }
        }

        public class BinaryOperationNode : Node
        {
            public Node Left { get; }
            public Token Operator { get; }
            public Node Right { get; }

            public BinaryOperationNode(Node left, Token op, Node right) : base(op.Position)
            {
                Left = left;
                Operator = op;
                Right = right;
            }

            public override object Run(VariableValueProvider variableValueProvider)
            {
                object a = Left.Run(variableValueProvider);
                object b = Right.Run(variableValueProvider);
                switch (Operator.Type)
                {
                    case TokenType.Plus:
                        {
                            if (IsNumeric(a, out var fa) && IsNumeric(b, out var fb)) return fa + fb;
                            if (a is string sa && b is string sb) return sa + sb;
                            break;
                        }
                    case TokenType.Minus:
                        {
                            if (IsNumeric(a, out var fa) && IsNumeric(b, out var fb)) return fa - fb;
                            break;
                        }
                    case TokenType.Multiply:
                        {
                            if (IsNumeric(a, out var fa) && IsNumeric(b, out var fb)) return fa * fb;
                            if (a is string sa && IsNumeric(b, out var fb2)) return string.Concat(Enumerable.Repeat(sa, Mathf.RoundToInt(fb2)));
                            break;
                        }
                    case TokenType.Divide:
                        {
                            if (IsNumeric(a, out var fa) && IsNumeric(b, out var fb)) return fa / fb;
                            break;
                        }
                    case TokenType.Power:
                        {
                            if (IsNumeric(a, out var fa) && IsNumeric(b, out var fb)) return Mathf.Pow(fa, fb);
                            break;
                        }
                    case TokenType.Number:
                    case TokenType.Variable:
                    case TokenType.LeftParen:
                    case TokenType.RightParen:
                    case TokenType.End:
                    default:
                        throw new InvalidOperationException(Operator.Type.ToString());
                }
                throw new InvalidOperationException($"{Operator.Type} between {a.GetType().FullName} and {b.GetType().FullName} at position {Position}");
            }

            public bool IsNumeric(object value, out float f)
            {
                switch (value)
                {
                    case float f1:
                        f = (float)f1;
                        return true;
                    case double f2:
                        f = (float)f2;
                        return true;
                    case int i:
                        f = (int)i;
                        return true;
                    case long l:
                        f = (long)l;
                        return true;
                    case string s:
                        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
                    default:
                        break;
                }
                f = 0;
                return false;
            }
        }
    }
}
