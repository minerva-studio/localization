using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Minerva.Localizations.EscapePatterns.ExpressionParser;

namespace Minerva.Localizations.EscapePatterns
{
    internal sealed class L10nEvaluator
    {
        private readonly EvaluationContext context;
        private readonly StringBuilder output;

        public L10nEvaluator(EvaluationContext context)
        {
            this.context = context;
            this.output = new StringBuilder();
        }

        public string Evaluate(List<L10nToken> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return string.Empty;

            foreach (var token in tokens)
            {
                EvaluateToken(token);
            }

            return output.ToString();
        }

        public string Evaluate(L10nToken token)
        {
            EvaluateToken(token);
            return output.ToString();
        }

        private void EvaluateToken(L10nToken token)
        {
            switch (token.Type)
            {
                case TokenType.Literal:
                    EvaluateLiteral(token);
                    break;

                case TokenType.KeyReference:
                    EvaluateKeyReference(token);
                    break;

                case TokenType.DynamicValue:
                    EvaluateDynamicValue(token);
                    break;

                case TokenType.ColorTag:
                    EvaluateColorTag(token);
                    break;
            }
        }

        private void EvaluateLiteral(L10nToken token)
        {
            output.Append(token.Content.Span);
            if (token.Children == null || token.Children.Count <= 0) return;

            foreach (var child in token.Children)
            {
                EvaluateToken(child);
            }
        }

        private void EvaluateKeyReference(L10nToken token)
        {
            var key = token.Content.ToString();
            var rawContent = L10n.GetRawContent(key);

            if (string.IsNullOrEmpty(rawContent))
            {
                output.Append(token.Content.Span);
                return;
            }

            if (context.CanRecurse())
            {
                var nestedContext = context.IncreaseDepth();
                var nestedTokens = new L10nTokenizer(rawContent).Tokenize();
                var resolved = new L10nEvaluator(nestedContext).Evaluate(nestedTokens);

                resolved = ApplyReferenceOptions(key, resolved, token.IsTooltip);
                output.Append(resolved);
            }
            else
            {
                Debug.LogWarning($"Max recursion depth reached for key: {key}");
                output.Append(rawContent);
            }
        }

        private void EvaluateDynamicValue(L10nToken token)
        {
            try
            {
                var exprSpan = token.Content.Span;
                var format = token.Metadata.Length > 0 ? token.Metadata.ToString() : string.Empty;

                // Fast path: check if it's a simple variable name (most common case)
                // Simple variable: alphanumeric + underscore + dot only, no operators
                if (IsSimpleVariableName(exprSpan))
                {
                    // Direct variable resolution without parser
                    var r = VariableResolver(token.Content);
                    AppendResult(r, format);
                    return;
                }

                // Complex expression: use full parser
                var expr = token.Content;
                var parser = new Parser(expr);
                var ast = parser.ParseExpression();
                var result = ast.Run(VariableResolver);

                AppendResult(result, format);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                output.Append(token.Content.Span);
            }
        }

        private void AppendResult(object result, string format)
        {
            if (EscapePattern.TryFormatNumber(result, out var formatted, format))
            {
                output.Append(formatted);
                return;
            }

            if (result is string str)
            {
                if (context.CanRecurse())
                {
                    var nestedContext = context.IncreaseDepth();
                    var nestedTokens = new L10nTokenizer(str).Tokenize();
                    var resolved = new L10nEvaluator(nestedContext).Evaluate(nestedTokens);
                    output.Append(resolved);
                }
                else
                {
                    output.Append(str);
                }
            }
            else
            {
                output.Append(result?.ToString() ?? "null");
            }
        }

        /// <summary>
        /// Check if expression is a simple variable name (fast path check)
        /// Simple variable: letters, digits, underscore, dot, angle brackets for params
        /// Examples: "name", "player.health", "damage<fire,level=2>"
        /// </summary>
        private static bool IsSimpleVariableName(ReadOnlySpan<char> expr)
        {
            if (expr.Length == 0)
                return false;

            bool inAngleBrackets = false;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                // Allowed: letters, digits, underscore, dot
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                    continue;

                // Handle angle brackets for parameters: name<param1,param2>
                if (c == '<')
                {
                    if (inAngleBrackets)
                        return false; // Nested angle brackets not allowed
                    inAngleBrackets = true;
                    continue;
                }

                if (c == '>')
                {
                    if (!inAngleBrackets)
                        return false; // Closing without opening
                    inAngleBrackets = false;
                    continue;
                }

                // Inside angle brackets, allow comma, equals, and whitespace
                if (inAngleBrackets && (c == ',' || c == '=' || char.IsWhiteSpace(c)))
                    continue;

                // Any other character makes it a complex expression
                // This includes: +, -, *, /, %, (, ), [, ], operators, etc.
                return false;
            }

            // Must close all angle brackets
            return !inAngleBrackets;
        }

        private void EvaluateColorTag(L10nToken token)
        {
            var colorCode = token.Metadata.ToString();

            if (colorCode.Length == 1)
            {
                colorCode = ColorCode.GetColorHex(colorCode[0]);
            }

            output.Append($"<color={colorCode}>");

            if (token.Children != null && token.Children.Count > 0)
            {
                foreach (var child in token.Children)
                {
                    EvaluateToken(child);
                }
            }
            else
            {
                output.Append(token.Content.Span);
            }

            output.Append("</color>");
        }

        private object VariableResolver(ReadOnlyMemory<char> varName)
        {
            var nameSpan = varName.Span;

            // Try direct lookup first (most common case, no allocation)
            if (context.Variables.TryGetValue(varName.ToString(), out var value))
            {
                return value;
            }

            if (context.Context == null)
            {
                return varName.ToString();
            }

            // Parse varName<param1,param2> structure manually
            int angleIndex = nameSpan.IndexOf('<');

            // No parameters - fast path (single allocation)
            if (angleIndex < 0)
            {
                return context.Context.GetEscapeValue(varName.ToString(), L10nParams.Empty);
            }

            // Has parameters - parse carefully
            if (nameSpan[^1] != '>')
            {
                // Malformed, fallback
                return context.Context.GetEscapeValue(varName.ToString(), L10nParams.Empty);
            }

            var key = varName[..angleIndex].ToString();
            var paramSpan = varName.Slice(angleIndex + 1, varName.Length - angleIndex - 2);

            // Empty parameters
            if (paramSpan.Length == 0)
            {
                return context.Context.GetEscapeValue(key, L10nParams.Empty);
            }

            // Parse parameters directly to L10nParams (zero-allocation parsing)
            var parameters = L10nParams.ParseParameters(paramSpan);
            return context.Context.GetEscapeValue(key, parameters);
        }

        private string ApplyReferenceOptions(string key, string content, bool isTooltip)
        {
            var option = isTooltip ? L10n.TooltipImportOption : L10n.ReferenceImportOption;

            bool withLink = option.HasFlag(ReferenceImportOption.WithLinkTag);
            bool withUnderline = option.HasFlag(ReferenceImportOption.WithUnderline);
            bool splitUnderline = withUnderline &&
                                  L10n.UseUnderlineResolver == UnderlineResolverOption.WhileLinking;

            if (!withLink && !withUnderline)
                return content;

            if (splitUnderline && content.Contains("<color"))
            {
                content = SplitUnderlineInline(content);
            }
            else if (withUnderline)
            {
                content = $"<u>{content}</u>";
            }

            if (withLink)
            {
                content = $"<link={key}>{content}</link>";
            }

            return content;
        }

        private static string SplitUnderlineInline(string content)
        {
            var sb = new StringBuilder(content.Length + 20);
            int pos = 0;

            while (pos < content.Length)
            {
                int colorStart = content.IndexOf("<color", pos, StringComparison.Ordinal);
                if (colorStart == -1)
                {
                    sb.Append(content.AsSpan(pos));
                    break;
                }

                sb.Append(content.AsSpan(pos, colorStart - pos));

                int colorTagEnd = content.IndexOf('>', colorStart);
                if (colorTagEnd == -1) break;

                sb.Append(content.AsSpan(colorStart, colorTagEnd - colorStart + 1));

                int colorEnd = content.IndexOf("</color>", colorTagEnd, StringComparison.Ordinal);
                if (colorEnd == -1) break;

                sb.Append("<u>");
                sb.Append(content.AsSpan(colorTagEnd + 1, colorEnd - colorTagEnd - 1));
                sb.Append("</u></color>");

                pos = colorEnd + 8;
            }

            return sb.ToString();
        }
    }
}