using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Minerva.Localizations
{
    /// <summary>
    /// Structured parameters for L10n queries (high-performance)
    /// </summary>
    public readonly struct L10nParams
    {
        public static L10nParams Empty => Create();

        private readonly Dictionary<string, object> variables;
        private readonly string[] options;

        /// <summary>
        /// Option segments (e.g., ["Daily", "desc"] for "Daily.desc")
        /// </summary>
        public IReadOnlyList<string> Options => options ?? Array.Empty<string>();

        /// <summary>
        /// Recursion depth (internal use)
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Variables dictionary (read-only view)
        /// </summary>
        public IReadOnlyDictionary<string, object> Variables => variables;

        #region Constructors

        private L10nParams(string[] options, int depth, Dictionary<string, object> vars)
        {
            this.options = options;
            Depth = depth;
            variables = vars;
        }

        internal L10nParams(string[] options, int depth, Dictionary<string, object> vars, bool _)
        {
            this.options = options;
            Depth = depth;
            variables = vars;
        }

        // Legacy single option constructor
        private L10nParams(string option, int depth, Dictionary<string, object> vars)
        {
            this.options = string.IsNullOrEmpty(option) ? null : new[] { option };
            Depth = depth;
            variables = vars;
        }

        // Internal constructor for depth changes (legacy)
        internal L10nParams(string option, int depth, Dictionary<string, object> vars, bool _)
        {
            this.options = string.IsNullOrEmpty(option) ? null : new[] { option };
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
            return new L10nParams((string[])null, 0, null);
        }

        /// <summary>
        /// Create parameters with specified option
        /// </summary>
        /// <param name="option">Option name (e.g., "name", "desc", "tooltip")</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static L10nParams Create(string option)
        {
            return new L10nParams(option, 0, null);
        }

        /// <summary>
        /// Create parameters with multiple option segments
        /// </summary>
        /// <param name="options">Option segments (e.g., "Daily", "desc")</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static L10nParams Create(params string[] options)
        {
            var filtered = options?.Where(o => !string.IsNullOrEmpty(o)).ToArray();
            return new L10nParams(filtered, 0, null);
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
        /// Change the option (single segment)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public L10nParams WithOption(string option)
        {
            return new L10nParams(option, Depth, variables);
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
        public bool TryGetVariable<T>(string key, out T value)
        {
            if (variables != null && variables.TryGetValue(key, out var obj))
            {
                if (obj is T typed)
                {
                    value = typed;
                    return true;
                }
                // 尝试类型转换
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
        public bool TryGetVariable(string key, out string value)
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
        public T GetVariableOrDefault<T>(string key, T defaultValue = default)
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
        /// → Final option: "Daily.desc"
        /// </remarks>
        public static L10nParams FromLegacy(params string[] param)
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

        public static L10nParams FromLegacy(string[] param, int depth)
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
            return new L10nParams(options, depth, vars.Count > 0 ? vars : null);
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

        private static bool ArrayEquals(string[] a, string[] b)
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

        private static int ArrayHashCode(string[] arr)
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

        private static bool DictionaryEquals(Dictionary<string, object> a, Dictionary<string, object> b)
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