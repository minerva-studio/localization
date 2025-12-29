#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Minerva.Localizations
{
    /// <summary>
    /// Structured parameters for L10n queries (high-performance)
    /// </summary>
    public readonly struct L10nParams
    {
        public static readonly L10nParams Empty = Create();

        private readonly Dictionary<string, object>? variables;
        private readonly string[]? options;

        /// <summary>
        /// Recursion depth (internal use)
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Option segments (e.g., ["Daily", "desc"] for "Daily.desc")
        /// </summary>
        public IReadOnlyList<string> Options => options ?? Array.Empty<string>();

        /// <summary>
        /// Variables dictionary (read-only view)
        /// </summary>
        public IReadOnlyDictionary<string, object>? Variables => variables;

        public bool IsEmpty => (options == null || options.Length == 0) && (variables == null || variables.Count == 0);

        #region Constructors

        private L10nParams(string[]? options, int depth, Dictionary<string, object>? vars)
        {
            this.options = options;
            Depth = depth;
            variables = vars;
        }

        #endregion

        #region Builders

        /// <summary>
        /// Create parameters with default empty option
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static L10nParams Create()
        {
            return new L10nParams((string[]?)null, 0, null);
        }

        /// <summary>
        /// Create parameters with multiple option segments
        /// </summary>
        /// <param name="options">Option segments (e.g., "Daily", "desc")</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static L10nParams Create(params string[] options)
        {
            return new L10nParams(options, 0, null);
        }

        /// <summary>
        /// Add a variable to parameters
        /// </summary>
        public L10nParams With(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                return this;

            var vars = variables != null
                ? new Dictionary<string, object>(variables)
                : new Dictionary<string, object>(4);
            vars[key] = value;
            return new L10nParams(options, Depth, vars);
        }

        /// <summary>
        /// Add multiple variables at once
        /// </summary>
        public L10nParams With(params (string key, object value)[] keyValues)
        {
            var result = this;
            foreach (var (key, value) in keyValues)
                result = result.With(key, value);
            return result;
        }

        /// <summary>
        /// Change the options (multiple segments)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public L10nParams WithOptions(params string[] options)
        {
            var filtered = options?.Where(o => !string.IsNullOrEmpty(o)).ToArray();
            return new L10nParams(filtered, Depth, variables);
        }

        /// <summary>
        /// Append additional option segments
        /// </summary>
        public L10nParams AppendOptions(params string[] additionalOptions)
        {
            if (additionalOptions == null || additionalOptions.Length == 0)
                return this;

            var filtered = additionalOptions.Where(o => !string.IsNullOrEmpty(o)).ToArray();
            if (filtered.Length == 0)
                return this;

            if (options == null || options.Length == 0)
                return new L10nParams(filtered, Depth, variables);

            var combined = new string[options.Length + filtered.Length];
            Array.Copy(options, 0, combined, 0, options.Length);
            Array.Copy(filtered, 0, combined, options.Length, filtered.Length);
            return new L10nParams(combined, Depth, variables);
        }

        /// <summary>
        /// Returns a new instance of <see cref="L10nParams"/> with the specified options prepended to the current
        /// options.
        /// </summary>
        /// <param name="additionalOptions">An array of option strings to prepend. The options are added in the order provided, before any existing
        /// options. Cannot be null.</param>
        /// <returns>A new <see cref="L10nParams"/> instance containing the prepended options followed by the existing options.</returns>
        public L10nParams PrependOptions(params string[] additionalOptions)
        {
            if (additionalOptions == null || additionalOptions.Length == 0)
                return this;

            var filtered = additionalOptions.Where(o => !string.IsNullOrEmpty(o)).ToArray();
            if (filtered.Length == 0)
                return this;

            if (options == null || options.Length == 0)
                return new L10nParams(filtered, Depth, variables);

            var combined = new string[filtered.Length + options.Length];
            Array.Copy(filtered, 0, combined, 0, filtered.Length);
            Array.Copy(options, 0, combined, filtered.Length, options.Length);
            return new L10nParams(combined, Depth, variables);
        }


        /// <summary>
        /// Increase recursion depth (internal use)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal L10nParams IncreaseDepth()
        {
            return new L10nParams(options, Depth + 1, variables);
        }

        #endregion

        #region Variable Access

        /// <summary>
        /// Try get a variable value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVariable<T>(string key, [NotNullWhen(true)] out T? value)
        {
            if (variables != null && variables.TryGetValue(key, out var obj))
            {
                if (obj is T typed)
                {
                    value = typed;
                    return true;
                }
                try
                {
                    value = (T)Convert.ChangeType(obj, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Try get a variable value as string (most common case)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVariable(string key, [NotNullWhen(true)] out string? value)
        {
            if (variables != null && variables.TryGetValue(key, out var obj))
            {
                value = obj?.ToString() ?? string.Empty;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Get variable or default
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetVariableOrDefault<T>(string key, T defaultValue = default!)
        {
            return TryGetVariable<T>(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Check if variable exists
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasVariable(string key)
        {
            return variables != null && variables.ContainsKey(key);
        }

        #endregion

        #region Options

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string option)
        {
            if (options == null || options.Length == 0)
                return false;
            foreach (var opt in options)
            {
                if (opt == option)
                    return true;
            }
            return false;
        }

        #endregion

        #region Zero-Allocation Parsing

        /// <summary>
        /// Parse parameters from ReadOnlyMemory with minimal allocations
        /// Supports: "opt1,opt2,key=value,opt3"
        /// </summary>
        public static L10nParams ParseParameters(ReadOnlyMemory<char> paramSpan)
        {
            var span = paramSpan.Span;

            if (span.Length == 0)
                return Empty;

            // Count commas to estimate capacity
            int commaCount = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == ',') commaCount++;
            }

            List<string>? optionList = null;
            Dictionary<string, object>? vars = null;

            int start = 0;
            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == ',')
                {
                    var segment = TrimSegment(span[start..i]);

                    if (segment.Length > 0)
                    {
                        // Check if it's a key=value pair
                        int eqIndex = segment.IndexOf('=');
                        if (eqIndex > 0)
                        {
                            vars ??= new Dictionary<string, object>(commaCount);
                            var key = new string(segment[..eqIndex]);
                            var value = new string(segment[(eqIndex + 1)..]);
                            vars[key] = value;
                        }
                        else
                        {
                            // It's an option
                            optionList ??= new List<string>(commaCount + 1);
                            optionList.Add(new string(segment));
                        }
                    }

                    start = i + 1;
                }
            }

            var options = optionList?.ToArray();
            return new L10nParams(options, 0, vars);
        }

        private static ReadOnlySpan<char> TrimSegment(ReadOnlySpan<char> span)
        {
            int start = 0;
            int end = span.Length;

            while (start < end && char.IsWhiteSpace(span[start]))
                start++;

            while (end > start && char.IsWhiteSpace(span[end - 1]))
                end--;

            return span[start..end];
        }

        #endregion

        #region Legacy Compatibility

        /// <summary>
        /// Parse legacy string[] parameters into L10nParams
        /// </summary>
        /// <remarks>
        /// Supports formats:
        /// - "key=value" → parsed as variable
        /// - Bare strings (without '=') → treated as option segments in order
        /// 
        /// Example: ["Daily", "level=2", "desc"] 
        /// → Options: ["Daily", "desc"], Variables: {"level": "2"}
        /// </remarks>
        public static L10nParams FromStrings(params string[] param)
        {
            if (param == null || param.Length == 0)
                return Create();

            var optionList = new List<string>(param.Length);
            var vars = new Dictionary<string, object>(param.Length);

            foreach (var p in param)
            {
                if (string.IsNullOrEmpty(p)) continue;

                // Try parse as key=value
                int idx = p.IndexOf('=');
                if (idx > 0)
                {
                    var key = p[..idx];
                    var value = p[(idx + 1)..];
                    vars[key] = value;
                    continue;
                }

                // Bare string without '=' is an option segment
                optionList.Add(p);
            }

            var options = optionList.Count > 0 ? optionList.ToArray() : null;
            return new L10nParams(options, 0, vars.Count > 0 ? vars : null);
        }

        public static L10nParams FromStrings(string[] param, int depth)
        {
            if (param == null || param.Length == 0)
                return new L10nParams(null, depth, null);

            var optionList = new List<string>(param.Length);
            var vars = new Dictionary<string, object>(param.Length);

            foreach (var p in param)
            {
                if (string.IsNullOrEmpty(p)) continue;

                // Try parse as key=value
                int idx = p.IndexOf('=');
                if (idx > 0)
                {
                    var key = p[..idx];
                    var value = p[(idx + 1)..];
                    vars[key] = value;
                    continue;
                }

                // Bare string without '=' is an option segment
                optionList.Add(p);
            }

            var options = optionList.Count > 0 ? optionList.ToArray() : null;
            return new L10nParams(options, depth, vars.Count > 0 ? vars : null);
        }

        public static L10nParams FromString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return Create();

            // check arg or option
            int idx = str.IndexOf('=');
            if (idx > 0)
            {
                var key = str[..idx];
                var value = str[(idx + 1)..];
                var vars = new Dictionary<string, object>(1)
                {
                    [key] = value
                };
                return new L10nParams(null, 0, vars);
            }
            else
            {
                return new L10nParams(new string[] { str }, 0, null);
            }
        }

        /// <summary>
        /// Convert to legacy string[] format (for backward compatibility)
        /// </summary>
        public string[] ToLegacy()
        {
            var result = new List<string>((options?.Length ?? 0) + (variables?.Count ?? 0));

            // Add option segments
            if (options != null)
            {
                result.AddRange(options);
            }

            // Add variables as key=value
            if (variables != null)
            {
                foreach (var kv in variables)
                {
                    result.Add($"{kv.Key}={kv.Value}");
                }
            }

            return result.ToArray();
        }

        #endregion

        #region Equality & ToString

        public override string ToString()
        {
            var optStr = options != null && options.Length > 0
                ? $"[{string.Join(", ", options.Select(o => $"\"{o}\""))}]"
                : "[]";
            return $"L10nParams {{ Options={optStr}, Depth={Depth}, Variables={variables?.Count ?? 0} }}";
        }

        public override bool Equals(object obj)
        {
            if (obj is L10nParams other)
            {
                return ArrayEquals(options, other.options) &&
                       Depth == other.Depth &&
                       DictionaryEquals(variables, other.variables);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ArrayHashCode(options);
                hash = hash * 31 + Depth;
                hash = hash * 31 + (variables?.Count ?? 0);
                return hash;
            }
        }

        private static bool ArrayEquals(string[]? a, string[]? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static int ArrayHashCode(string[]? arr)
        {
            if (arr == null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var item in arr)
                {
                    hash = hash * 31 + (item?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        private static bool DictionaryEquals(Dictionary<string, object>? a, Dictionary<string, object>? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var bVal))
                    return false;
                if (!Equals(kv.Value, bVal))
                    return false;
            }
            return true;
        }

        #endregion
    }
}