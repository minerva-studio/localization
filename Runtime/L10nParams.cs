using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Minerva.Localizations
{
    /// <summary>
    /// Structured parameters for L10n queries (high-performance)
    /// </summary>
    public readonly struct L10nParams
    {
        private readonly Dictionary<string, object> variables;

        /// <summary>
        /// Option type (e.g., "name", "desc", "info", or custom)
        /// </summary>
        public string Option { get; }

        /// <summary>
        /// Recursion depth (internal use)
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Variables dictionary (read-only view)
        /// </summary>
        public IReadOnlyDictionary<string, object> Variables => variables;

        #region Constructors

        // 私有构造函数
        private L10nParams(string option, int depth, Dictionary<string, object> vars)
        {
            Option = option ?? string.Empty;
            Depth = depth;
            variables = vars;
        }

        // Internal 构造函数供内部使用
        internal L10nParams(string option, int depth, Dictionary<string, object> vars, bool _)
        {
            Option = option ?? string.Empty;
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
            return new L10nParams(string.Empty, 0, null);
        }

        /// <summary>
        /// Create parameters with specified option
        /// </summary>
        /// <param name="option">Option name (e.g., "name", "desc", "tooltip")</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static L10nParams Create(string option)
        {
            return new L10nParams(option ?? string.Empty, 0, null);
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
            return new L10nParams(Option, Depth, vars);
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
        /// Change the option
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public L10nParams WithOption(string option)
        {
            return new L10nParams(option ?? string.Empty, Depth, variables);
        }

        /// <summary>
        /// Increase recursion depth (internal use)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal L10nParams IncreaseDepth()
        {
            return new L10nParams(Option, Depth + 1, variables);
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
        /// - First non-key=value string → treated as option
        /// - "key=value" → parsed as variable
        /// - Bare strings → treated as option if first, ignored otherwise
        /// </remarks>
        public static L10nParams FromLegacy(params string[] param)
        {
            if (param == null || param.Length == 0)
                return Create();

            string option = string.Empty;
            var vars = new Dictionary<string, object>(param.Length);
            bool optionSet = false;

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

                // First bare string is option
                if (!optionSet)
                {
                    option = p;
                    optionSet = true;
                }
                // Other bare strings are ignored (or can be treated as positional args)
            }

            return new L10nParams(option, 0, vars.Count > 0 ? vars : null);
        }

        /// <summary>
        /// Convert to legacy string[] format (for backward compatibility)
        /// </summary>
        public string[] ToLegacy()
        {
            var result = new List<string>(variables?.Count ?? 0 + 1);

            // Add option as first element
            if (!string.IsNullOrEmpty(Option))
                result.Add(Option);

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
            return $"L10nParams {{ Option=\"{Option}\", Depth={Depth}, Variables={variables?.Count ?? 0} }}";
        }

        public override bool Equals(object obj)
        {
            if (obj is L10nParams other)
            {
                return Option == other.Option &&
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
                hash = hash * 31 + (Option?.GetHashCode() ?? 0);
                hash = hash * 31 + Depth;
                hash = hash * 31 + (variables?.Count ?? 0);
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