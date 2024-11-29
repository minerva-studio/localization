using Minerva.Localizations.EscapePatterns;
using System;
using System.Collections;
using System.Collections.Generic;
using static Minerva.Localizations.Localizable;
using static Minerva.Localizations.EscapePatterns.EscapePattern;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Base class of the context of localization
    /// </summary>
    public abstract class L10nContext : ILocalizable, ILocalizer
    {
        private static Dictionary<string, DynamicValueProvider> globalEscapeValues = new();

        private Key key;
        private object value;
        private Dictionary<string, DynamicValueProvider> localEscapeValues;


        /// <summary>
        /// Get the base localization key of the object
        /// </summary> 
        public string BaseKeyString { get => key; protected set { key = new Key(value); } }
        public Key BaseKey { get => key; protected set { key = value; } }
        public object BaseValue { get => value; set => this.value = value; }
        public Dictionary<string, DynamicValueProvider> LocalEscapeValue => localEscapeValues ??= new();
        public static Dictionary<string, DynamicValueProvider> GlobalEscapeValue => globalEscapeValues ??= new();


        /// <summary>
        /// The default constructor, might called from L10nContext.Of()
        /// </summary>
        protected L10nContext() { }

        protected L10nContext(object value)
        {
            Init(value);
        }

        protected L10nContext(object value, string key)
        {
            Init(value, key);
        }

        protected L10nContext(object value, Key key)
        {
            Init(value, key);
        }

        private void Init(object value) => Init(value, Key.Empty);
        private void Init(object value, string key) => Init(value, new Key(key));
        private void Init(object value, Key key)
        {
            this.value = value;
            this.key = key;
            Parse(value);
        }

        /// <summary>
        /// Method of how to parse the given object value into desired way
        /// </summary>
        /// <param name="value"></param>
        protected virtual void Parse(object value)
        {
            this.key = value is string str ? str : (value?.GetType().FullName ?? string.Empty);
        }




        /// <summary>
        /// Get the key represent for this localizable object
        /// <br></br>
        /// Override to get keys from the object
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual Key GetLocalizationKey(params string[] param)
        {
            return Localizable.AppendKey(key, param);
        }

        public static Key AppendKey(Key key, params string[] param)
        {
            return Localizable.AppendKey(key, param);
        }

        /// <summary>
        /// Get the raw content, override this for creating custom format of localized content
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual string GetRawContent(params string[] param)
        {
            var key = GetLocalizationKey(param);
            var rawString = L10n.GetRawContent(key);
            return rawString;
        }

        /// <summary>
        /// Get escape value from the object
        /// </summary>
        /// <param name="escapeKey"></param>
        /// <returns></returns>
        public virtual object GetEscapeValue(string escapeKey, params string[] param)
        {
            if (HasLocalEscapeValue(escapeKey, out var v)) return v(escapeKey, param);
            if (HasGlobalEscapeValue(escapeKey, out var global)) return global(escapeKey, param);
            var value = GetObjectNullPropagation(this.value, escapeKey);
            if (value == null) return escapeKey;
            return DynamicValueOf(value, param);
        }

        /// <summary>
        /// <inheritdoc/> 
        /// <br/>
        /// Short hand for <see cref="L10n.Tr(ILocalizable, string[])"/>
        /// </summary>
        /// <returns>
        /// <inheritdoc/>
        /// </returns>
        public string Tr(params string[] param)
        {
            return Localizable.Tr(this, param);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <br/>
        /// Short hand for <see cref="L10n.TrKey(string, ILocalizable, string[])"/>
        /// </summary>
        /// <returns>
        /// <inheritdoc/>
        /// </returns>
        public string TrKey(string overrideKey, params string[] param)
        {
            return Localizable.TrKey(overrideKey, this, param);
        }



        /// <summary>
        /// Get all possible option of this content
        /// </summary>
        /// <param name="firstLevelOnly"></param>
        /// <returns></returns>
        public string[] GetOptions(bool firstLevelOnly = false, params string[] options)
        {
            var key = BaseKey;
            foreach (var item in options ?? Array.Empty<string>())
            {
                key.Append(item);
            }
            return L10n.OptionOf(key, firstLevelOnly);
        }

        /// <summary>
        /// Get dynamic values
        /// </summary>
        /// <returns></returns>
        public List<string> GetDynamicValues()
        {
            var l = new List<string>();
            GetDynamicValues(l);
            return l;
        }

        /// <summary>
        /// Get dynamic values
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public virtual int GetDynamicValues(List<string> list)
        {
            System.Reflection.Assembly assembly = typeof(UnityEngine.Object).Assembly;
            list.Clear();
            if (value == null) return 0;
            var type = value.GetType();
            var fields = type.GetFields();
            foreach (var item in fields)
            {
                if (item.DeclaringType.Assembly != assembly)
                    list.Add(item.Name);
            }
            var properties = type.GetProperties();
            foreach (var item in properties)
            {
                if (item.DeclaringType.Assembly != assembly && item.CanRead)
                    list.Add(item.Name);
            }
            return list.Count;
        }





        public bool HasLocalEscapeValue(string escapeKey, out DynamicValueProvider v)
        {
            v = null;
            return localEscapeValues != null && LocalEscapeValue.TryGetValue(escapeKey, out v);
        }

        public static bool HasGlobalEscapeValue(string escapeKey, out DynamicValueProvider v)
        {
            v = null;
            return globalEscapeValues != null && GlobalEscapeValue.TryGetValue(escapeKey, out v);
        }





        public static string AsKeyEscape(string baseKey, params string[] args)
        {
            return EscapePattern.AsKeyEscape(baseKey, args);
        }

        /// <summary>
        /// Try get the localization of given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string LocalizationOf(object value, params string[] param)
        {
            return Localizable.Tr(value, param);
        }

        /// <summary>
        /// Try get the localization of given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object DynamicValueOf(object value, params string[] param)
        {
            // null value, return emtpy string
            if (value == null)
            {
                return string.Empty;
            }
            if (IsNumber(value))
            {
                return value;
            }
            // raw value, return value directly
            if (IsRawValue(value))
            {
                return value;
            }
            return Localizable.Tr(value, param);
        }

        /// <summary>
        /// Split action parameter
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>> GetOpt(params string[] param)
        {
            foreach (var item in param)
            {
                int index = item.IndexOf("=");
                if (index != -1)
                {
                    yield return new KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(item.AsMemory(..index), item.AsMemory((index + 1)..));
                }
                else yield return new KeyValuePair<ReadOnlyMemory<char>, ReadOnlyMemory<char>>(item.AsMemory(), null);
            }
        }





        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected static object GetObjectNullPropagation(object obj, string path) => Reflection.GetObjectNullPropagation(obj, path.AsMemory());

        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected static object GetObjectNullPropagation(object obj, ReadOnlyMemory<char> path) => Reflection.GetObjectNullPropagation(obj, path);

        /// <summary>
        /// Get the last object in the path that is not null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected static object GetObject(object obj, string path) => Reflection.GetObject(obj, path.AsMemory());

        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected static string Tr(string key, params string[] param)
        {
            return L10n.Tr(key, param);
        }

        /// <summary>
        /// Direct localization from key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected static string Tr(Key key, params string[] param)
        {
            return L10n.Tr(key, param);
        }

        /// <summary>
        /// Create a L10n context
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static L10nContext Of(object value)
        {
            if (value == null)
            {
                return new NoContext();
            }
            Type valueType = value.GetType();
            return Of(value, valueType);
        }

        private static L10nContext Of(object value, Type type)
        {
            if (type == typeof(DynamicContext)) return ((DynamicContext)(object)value).Clone();
            if (type.IsSubclassOf(typeof(L10nContext))) return new DynamicContext(value);
            var l10nContext = ContextTable.GetContextBuilder(type)();
            l10nContext.Init(value);
            return l10nContext;
        }

        /// <summary>
        /// Create a L10n context
        /// </summary>
        /// <remarks>
        /// cannot create a context object for a context object, it will always returns a dynamic context
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static L10nContext Of<T>(object value)
        {
            return Of(value, typeof(T));
        }

        /// <summary>
        /// Create a L10n context
        /// </summary>
        /// <param name="baseKey"></param>
        /// <returns></returns>
        public static L10nContext Of(string baseKey, params string[] args)
        {
            return new KeyL10nContext(Localizable.AppendKey(baseKey, args));
        }

        /// <summary>
        /// Create a L10n no-context context
        /// </summary>
        /// <returns></returns>
        public static L10nContext None()
        {
            return new NoContext();
        }

        private class NoContext : L10nContext
        {
            protected override void Parse(object value) { }
        }

        /// <summary>
        /// Register given l10n context type to target type
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="allowInheritance">does child classes also applied?</param>
        public static void Register<TContext, TTarget>(bool allowInheritance = false) where TContext : L10nContext, new()
        {
            ContextTable.Register<TContext, TTarget>(allowInheritance);
        }
    }
}