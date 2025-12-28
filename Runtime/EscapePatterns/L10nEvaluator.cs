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

                // Apply link/underline options
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
                var varName = token.Content.ToString();
                var format = token.Metadata.Length > 0 ? token.Metadata.ToString() : string.Empty;
                var parameters = token.Parameters.Length > 0 ? token.Parameters.ToString() : null;

                // ✅ 解析参数列表
                string[] localParams = null;
                if (!string.IsNullOrEmpty(parameters))
                {
                    localParams = ParseParameterList(parameters);
                }

                // ✅ 如果是表达式，使用 Parser
                if (IsExpression(varName))
                {
                    var parser = new Parser(varName);
                    var ast = parser.ParseExpression();
                    var result = ast.Run(VariableResolver);

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
                else
                {
                    // ✅ 简单变量名，直接从 context 获取
                    var value = ResolveVariable(varName, localParams);

                    if (EscapePattern.TryFormatNumber(value, out var formatted, format))
                    {
                        output.Append(formatted);
                        return;
                    }

                    if (value is string str)
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
                        output.Append(value?.ToString() ?? "null");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                output.Append(token.Content.Span);
            }
        }

        private void EvaluateColorTag(L10nToken token)
        {
            var colorCode = token.Metadata.ToString();

            // Convert simple color code (e.g., 'R') to hex
            if (colorCode.Length == 1)
            {
                colorCode = ColorCode.GetColorHex(colorCode[0]);
            }

            // Apply UGUI color tag
            output.Append($"<color={colorCode}>");

            // Handle Children
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

        // ✅ 新方法：判断是否为表达式
        private bool IsExpression(string input)
        {
            // 如果包含运算符，视为表达式
            return input.Contains('+') || input.Contains('-') ||
                   input.Contains('*') || input.Contains('/') ||
                   input.Contains('(') || input.Contains(')');
        }

        // ✅ 新方法：解析参数列表
        private string[] ParseParameterList(string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return Array.Empty<string>();

            var result = new List<string>();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                char c = parameters[i];

                if (c == '<' || c == '(') depth++;
                else if (c == '>' || c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(parameters.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            if (start < parameters.Length)
            {
                result.Add(parameters.Substring(start).Trim());
            }

            return result.ToArray();
        }

        // ✅ 新方法：解析变量（支持参数）
        private object ResolveVariable(string varName, string[] localParams)
        {
            // 优先从 Variables 查找
            if (context.Variables.TryGetValue(varName, out var value))
            {
                return value;
            }

            // 从 Context 查找，传递参数
            if (context.Context != null)
            {
                return context.Context.GetEscapeValue(varName, localParams ?? Array.Empty<string>());
            }

            return varName;
        }

        private object VariableResolver(ReadOnlyMemory<char> varName)
        {
            var name = varName.ToString();
            return ResolveVariable(name, null);
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