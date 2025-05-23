﻿using System;
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

            public Lexer(string input)
            {
                _input = input.AsMemory();
            }

            public Token GetNextToken()
            {
                SkipWhitespace();

                if (_position >= _input.Length)
                    return new Token(TokenType.End, ReadOnlyMemory<char>.Empty, _position);

                char current = _input.Span[_position];

                if (IsValidTokenInNumber(current))
                    return NumberToken();
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

                throw new Exception($"Unexpected character: {current}");
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

                while (_position < _input.Length && (char.IsDigit(_input.Span[_position]) || _input.Span[_position] == '.'))
                {
                    if (_input.Span[_position] == '.')
                    {
                        if (hasDot) // Multiple dots are invalid
                            throw new Exception($"Invalid number format at position {start}");
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
                    throw new Exception($"Invalid variable format '{span}' at position {start}");

                return new Token(TokenType.Variable, span, start);
            }

            private bool IsValidTokenInNumber(char c)
            {
                if (char.IsDigit(c)) return true;
                if (c == '-') return true;
                if (c == '.') return true;
                return false;
            }

            private bool IsValidTokenInVariable(char c)
            {
                if (char.IsLetterOrDigit(c)) return true;
                switch (c)
                {
                    case '<':
                    case '>':
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

            public Parser(string input)
            {
                _lexer = new Lexer(input);
                _currentToken = _lexer.GetNextToken();
            }

            // Parse the expression
            public Node ParseExpression()
            {
                return ParseTerm();
            }

            private Node ParseTerm()
            {
                Node node = ParseFactor();

                while (_currentToken.Type == TokenType.Plus || _currentToken.Type == TokenType.Minus)
                {
                    Token token = _currentToken;
                    AdvanceToken();
                    node = new BinaryOperationNode(node, token, ParseFactor());
                    if (_currentToken.Type == TokenType.End) break;
                }

                return node;
            }

            private Node ParseFactor()
            {
                Node node = ParseExponentiation();

                while (_currentToken.Type == TokenType.Multiply || _currentToken.Type == TokenType.Divide)
                {
                    Token token = _currentToken;
                    AdvanceToken();
                    node = new BinaryOperationNode(node, token, ParseExponentiation());
                    if (_currentToken.Type == TokenType.End) break;
                }

                return node;
            }

            private Node ParseExponentiation()
            {
                Node node = ParsePrimary();

                while (_currentToken.Type == TokenType.Power)
                {
                    Token token = _currentToken;
                    AdvanceToken();
                    node = new BinaryOperationNode(node, token, ParsePrimary());
                    if (_currentToken.Type == TokenType.End) break;
                }

                return node;
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
                    Node node = ParseExpression();
                    if (_currentToken.Type != TokenType.RightParen)
                        throw new Exception($"Missing closing parenthesis at position {_currentToken.Position}");
                    AdvanceToken();
                    return node;
                }

                throw new Exception($"Invalid syntax at position {_currentToken.Position} ({_currentToken})");
            }

            private void AdvanceToken()
            {
                _currentToken = _lexer.GetNextToken();
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
                            if (IsNumeric(a, out var fa) && IsNumeric(b, out var fb)) return fa + fb;
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
                throw new InvalidOperationException($"{Operator.Type} between {a.GetType().FullName} and {b.GetType().FullName} (Position {Left.Position})");
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