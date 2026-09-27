using System;
using System.Collections;
using System.Globalization;
using System.Text;
using Minerva.Localizations.Utilities;

namespace Minerva.Localizations.EscapePatterns
{
    internal sealed class L10nEvaluator
    {
        private const int InitialStackCapacity = 16;
        private static readonly char[] NestedMarkers = { '{', '$', '§', '\\' };

        [ThreadStatic] private static L10nEvaluator[] evaluators;
        [ThreadStatic] private static int rentDepth;

        private EvaluationContext context;
        private readonly L10nEvaluationDiagnostics diagnostics = new();
        private L10nValue[] stack = new L10nValue[InitialStackCapacity];
        private int[] indices = new int[InitialStackCapacity];
        private string[] pathKeys = new string[InitialStackCapacity];
        private bool[] colorStack = new bool[InitialStackCapacity];

        private L10nEvaluator(in EvaluationContext context) => Reset(context);

        public static L10nEvaluator Rent(in EvaluationContext context)
        {
            evaluators ??= new L10nEvaluator[4];
            int slot = rentDepth++;
            if (slot >= evaluators.Length) Array.Resize(ref evaluators, evaluators.Length * 2);
            var evaluator = evaluators[slot] ??= new L10nEvaluator(context);
            evaluator.Reset(context);
            return evaluator;
        }

        public static void Return(L10nEvaluator evaluator)
        {
            evaluator?.Clear();
            if (rentDepth > 0) rentDepth--;
        }

        public void Reset(in EvaluationContext newContext)
        {
            context = newContext;
            diagnostics.Clear();
        }

        public void Clear()
        {
            context = default;
            colorDepth = 0;
            diagnostics.Clear();
            Array.Clear(stack, 0, stack.Length);
            // Evaluators live in a thread-static array; drop key references so evicted path keys can be collected.
            Array.Clear(pathKeys, 0, pathKeys.Length);
        }

        public L10nEvaluationDiagnostics GetDiagnostics() => diagnostics;

        internal void Evaluate(L10nTemplate template, StringBuilder output)
        {
            if (template == null || output == null) return;
            EvaluateTemplate(template, output);
        }

        private void EvaluateTemplate(L10nTemplate template, StringBuilder output)
        {
            int colorBase = 0;
            foreach (var op in template.Ops)
            {
                switch (op.Code)
                {
                    case L10nTemplate.OpCode.Literal:
                        output.Append(op.Text);
                        break;
                    case L10nTemplate.OpCode.KeyReference:
                        EvaluateKeyReference(op, output);
                        break;
                    case L10nTemplate.OpCode.Expression:
                        EvaluateExpressionOutput(op.Expression, op.Format, output);
                        break;
                    case L10nTemplate.OpCode.ColorOpen:
                    {
                        string color = ResolveColorCode(op.Text);
                        bool wrap = !string.IsNullOrEmpty(color);
                        PushColor(wrap);
                        if (wrap) output.Append("<color=").Append(color).Append('>');
                        colorBase++;
                        break;
                    }
                    case L10nTemplate.OpCode.ColorClose:
                        if (colorBase > 0 && PopColor()) output.Append("</color>");
                        if (colorBase > 0) colorBase--;
                        break;
                }
            }
        }

        private void EvaluateKeyReference(L10nTemplate.Op op, StringBuilder output)
        {
            string key = op.Text;
            string raw = L10n.GetRawContent(key);
            if (string.IsNullOrEmpty(raw))
            {
                diagnostics.AddError(L10nErrorSeverity.Warning, string.Concat("$", key, "$"), "KeyNotFound", $"Localization key '{key}' not found, using key as fallback");
                output.Append(key);
                return;
            }
            if (!context.CanRecurse())
            {
                diagnostics.AddError(L10nErrorSeverity.Warning, string.Concat("$", key, "$"), "RecursionDepth", $"Max recursion depth reached for key '{key}'");
                output.Append(raw);
                return;
            }

            var previous = context;
            context = context.IncreaseDepth();
            try
            {
                var nested = L10nTemplate.GetOrCompile(raw);
                var options = op.Flag ? L10n.TooltipImportOption : L10n.ReferenceImportOption;
                bool withLink = options.HasFlag(ReferenceImportOption.WithLinkTag);
                bool withUnderline = options.HasFlag(ReferenceImportOption.WithUnderline);
                if (!withLink && !withUnderline)
                {
                    EvaluateTemplate(nested, output);
                }
                else
                {
                    var nestedOutput = L10nObjectPool.RentStringBuilder();
                    try
                    {
                        EvaluateTemplate(nested, nestedOutput);
                        output.Append(ApplyReferenceOptions(key, nestedOutput.ToString(), options));
                    }
                    finally { L10nObjectPool.ReturnStringBuilder(nestedOutput); }
                }
            }
            finally { context = previous; }
        }

        private void EvaluateExpressionOutput(L10nExpression expression, string format, StringBuilder output)
        {
            if (expression.Error != null)
            {
                diagnostics.AddError(L10nErrorSeverity.Error, expression.Source, "SyntaxError", expression.Error);
                output.Append(expression.Source);
                return;
            }

            try
            {
                L10nValue result = EvaluateExpression(expression, 0, 0);
                L10nValue.ValueKind kind = result.Kind;
                if (kind == L10nValue.ValueKind.Number)
                {
                    output.Append(EscapePattern.FormatNumber(result.Number, format));
                    return;
                }
                if (kind == L10nValue.ValueKind.String && EscapePattern.TryFormatNumber(result.GetReference(kind), out string formatted, format))
                {
                    output.Append(formatted);
                    return;
                }
                AppendResult(result, output);
            }
            catch (Exception e)
            {
                diagnostics.AddError(L10nErrorSeverity.Error, expression.Source, "EvaluationError", e.Message, e);
                output.Append(expression.Source);
            }
        }

        internal L10nValue EvaluateExpression(L10nExpression expression, int stackBase = 0, int indexBase = 0)
        {
            EnsureStack(stackBase + expression.MaxStack);
            int top = 0;
            foreach (var op in expression.Ops)
            {
                switch (op.Code)
                {
                    case L10nOp.OpCode.PushNumber:
                        stack[stackBase + top++] = L10nValue.FromNumber(expression.Numbers[op.Operand]);
                        break;
                    case L10nOp.OpCode.LoadPath:
                    {
                        L10nValue loaded = LoadPath(expression.Paths[op.Operand], stackBase + top, indexBase);
                        stack[stackBase + top++] = loaded;
                        break;
                    }
                    case L10nOp.OpCode.Negate:
                    {
                        if (!stack[stackBase + top - 1].TryGetNumber(out double unary)) throw new InvalidOperationException("Unary '-' requires a numeric value.");
                        stack[stackBase + top - 1] = L10nValue.FromNumber(-unary);
                        break;
                    }
                    default:
                    {
                        L10nValue right = stack[stackBase + --top];
                        L10nValue left = stack[stackBase + top - 1];
                        stack[stackBase + top - 1] = Apply(op.Code, left, right);
                        break;
                    }
                }
            }
            return top == 0 ? L10nValue.FromObject(null) : stack[stackBase];
        }

        private L10nValue Apply(L10nOp.OpCode code, L10nValue left, L10nValue right)
        {
            L10nValue.ValueKind leftKind = left.Kind;
            L10nValue.ValueKind rightKind = right.Kind;
            if (left.TryGetNumber(leftKind, out double a) && right.TryGetNumber(rightKind, out double b))
            {
                return code switch
                {
                    L10nOp.OpCode.Add => L10nValue.FromNumber(a + b),
                    L10nOp.OpCode.Subtract => L10nValue.FromNumber(a - b),
                    L10nOp.OpCode.Multiply => L10nValue.FromNumber(a * b),
                    L10nOp.OpCode.Divide => L10nValue.FromNumber(a / b),
                    L10nOp.OpCode.Power => L10nValue.FromNumber(Math.Pow(a, b)),
                    _ => throw new InvalidOperationException(code.ToString())
                };
            }
            if (code == L10nOp.OpCode.Add && leftKind == L10nValue.ValueKind.String && rightKind == L10nValue.ValueKind.String)
                return L10nValue.FromString(string.Concat(left.GetReference(leftKind), right.GetReference(rightKind)));
            if (code == L10nOp.OpCode.Multiply && leftKind == L10nValue.ValueKind.String && right.TryGetNumber(rightKind, out double count))
            {
                int length = checked((int)Math.Round(count));
                if (length < 0) throw new ArgumentOutOfRangeException(nameof(count));
                string repeated = (string)left.GetReference(leftKind);
                var builder = new StringBuilder(repeated.Length * length);
                for (int i = 0; i < length; i++) builder.Append(repeated);
                return L10nValue.FromString(builder.ToString());
            }
            throw new InvalidOperationException($"Operator {code} is not defined for {left.ToObject()?.GetType().FullName ?? "null"} and {right.ToObject()?.GetType().FullName ?? "null"}.");
        }

        private L10nValue LoadPath(L10nPath path, int stackBase, int indexBase)
        {
            EnsureIndices(indexBase + path.SegmentCount * 2 + 8);
            EnsurePathKeys(path.SegmentCount);
            for (int i = 0; i < path.SegmentCount; i++)
            {
                // Dynamic subexpressions use a disjoint scratch range so they cannot overwrite indices already computed for this path.
                var segment = path.GetSegment(i);
                if (segment.Kind != L10nPath.SegmentKind.DynamicIndex) continue;
                var value = EvaluateExpression(segment.Expression, stackBase, indexBase + path.SegmentCount + 4);
                if (!TryConvertIndex(value, out int index)) throw new FormatException($"Index at path segment {i} must be a finite Int32 value.");
                indices[indexBase + i] = index;
            }

            path.BuildPrefixKeys(indices, indexBase, pathKeys);
            var variables = context.Parameters.Variables;
            L10nParams lookupParameters = path.Args ?? context.LookupParameters;
            if (variables != null)
            {
                for (int i = path.SegmentCount - 1; i >= 0; i--)
                {
                    if (!variables.TryGetValue(pathKeys[i], out var root)) continue;
                    if (path.TryWalk(root, i + 1, indices.AsSpan(indexBase), out var resolved)) return ResolveValue(resolved, lookupParameters);
                    break;
                }
            }

            if (context.Context != null)
            {
                for (int i = path.SegmentCount - 1; i >= 0; i--)
                {
                    if (!context.Context.TryGetEscapeValue(pathKeys[i], lookupParameters, out var root)) continue;
                    if (path.TryWalk(root, i + 1, indices.AsSpan(indexBase), out var resolved)) return ResolveValue(resolved, lookupParameters);
                }
            }
            else
            {
                var providers = L10nContext.GlobalEscapeValue;
                for (int i = path.SegmentCount - 1; i >= 0; i--)
                {
                    string key = pathKeys[i];
                    if (!providers.TryGetValue(key, out var provider)) continue;
                    L10nValue root = L10nValue.FromObject(provider(key, lookupParameters));
                    if (path.TryWalk(root, i + 1, indices.AsSpan(indexBase), out var resolved)) return ResolveValue(resolved, lookupParameters);
                }
            }

            string unresolved = pathKeys[path.SegmentCount - 1];
            diagnostics.AddError(L10nErrorSeverity.Warning, unresolved, "UnresolvedPath", $"Could not resolve localization path '{unresolved}'.");
            return L10nValue.FromString(unresolved);
        }

        private static L10nValue ResolveValue(in L10nValue value, L10nParams lookupParameters) => L10nContext.DynamicValueOf(value, lookupParameters);

        private static bool TryConvertIndex(L10nValue value, out int index)
        {
            index = 0;
            L10nValue.ValueKind kind = value.Kind;
            if (kind == L10nValue.ValueKind.Number)
            {
                double number = value.Number;
                if (!double.IsNaN(number) && !double.IsInfinity(number) && number >= int.MinValue && number <= int.MaxValue && Math.Truncate(number) == number)
                {
                    index = (int)number;
                    return true;
                }
                return false;
            }
            if (kind == L10nValue.ValueKind.String)
            {
                string text = (string)value.GetReference(kind);
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) return true;
                    if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed >= int.MinValue && parsed <= int.MaxValue && decimal.Truncate(parsed) == parsed)
                    { index = (int)parsed; return true; }
                    return false;
            }
            return false;
        }

        private void AppendResult(L10nValue value, StringBuilder output)
        {
            object result = value.ToObject();
            if (result is string str)
            {
                if (str.IndexOfAny(NestedMarkers) < 0 || !context.CanRecurse())
                {
                    output.Append(str);
                    return;
                }
                var previous = context;
                context = context.IncreaseDepth();
                try { EvaluateTemplate(L10nTemplate.GetOrCompile(str), output); }
                finally { context = previous; }
                return;
            }
            output.Append(result?.ToString() ?? "null");
        }

        private void PushColor(bool value)
        {
            if (colorStack.Length == 0) Array.Resize(ref colorStack, InitialStackCapacity);
            int slot = colorDepth++;
            if (slot >= colorStack.Length) Array.Resize(ref colorStack, colorStack.Length * 2);
            colorStack[slot] = value;
        }

        private int colorDepth;
        private bool PopColor() => colorDepth > 0 && colorStack[--colorDepth];

        private void EnsureStack(int capacity) { if (capacity > stack.Length) Array.Resize(ref stack, Math.Max(capacity, stack.Length * 2)); }
        private void EnsureIndices(int capacity) { if (capacity > indices.Length) Array.Resize(ref indices, Math.Max(capacity, indices.Length * 2)); }
        private void EnsurePathKeys(int capacity) { if (capacity > pathKeys.Length) Array.Resize(ref pathKeys, Math.Max(capacity, pathKeys.Length * 2)); }

        private static string ResolveColorCode(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode)) return null;
            if (colorCode.Length == 1) return ColorCode.GetColorHex(colorCode[0]);
            if (colorCode[0] == '#') return colorCode;
            if (colorCode[0] == '<' && colorCode[^1] == '>') return ColorResolvers.Resolve(colorCode.Substring(1, colorCode.Length - 2));
            return colorCode;
        }

        private static string ApplyReferenceOptions(string key, string content, ReferenceImportOption options)
        {
            bool withLink = options.HasFlag(ReferenceImportOption.WithLinkTag);
            bool withUnderline = options.HasFlag(ReferenceImportOption.WithUnderline);
            if (withUnderline && L10n.UseUnderlineResolver == UnderlineResolverOption.WhileLinking && content.Contains("<color"))
                content = SplitUnderlineInline(content);
            else if (withUnderline) content = $"<u>{content}</u>";
            return withLink ? $"<link={key}>{content}</link>" : content;
        }

        private static string SplitUnderlineInline(string content)
        {
            var builder = new StringBuilder(content.Length + 20);
            int position = 0;
            while (position < content.Length)
            {
                int colorStart = content.IndexOf("<color", position, StringComparison.Ordinal);
                if (colorStart < 0) { builder.Append(content.AsSpan(position)); break; }
                builder.Append(content.AsSpan(position, colorStart - position));
                int tagEnd = content.IndexOf('>', colorStart);
                if (tagEnd < 0) break;
                builder.Append(content.AsSpan(colorStart, tagEnd - colorStart + 1));
                int colorEnd = content.IndexOf("</color>", tagEnd, StringComparison.Ordinal);
                if (colorEnd < 0) break;
                builder.Append("<u>").Append(content.AsSpan(tagEnd + 1, colorEnd - tagEnd - 1)).Append("</u></color>");
                position = colorEnd + 8;
            }
            return builder.ToString();
        }
    }
}
