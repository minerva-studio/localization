using Minerva.Module;
using System.Collections.Generic;
using System.Linq;

namespace Amlos.Localizations
{
    /// <summary>
    /// Common interface use to get the localization information from an object
    /// </summary>
    public interface ILocalizable
    {
        /// <summary>
        /// Get the key represent for this localizable object
        /// <br></br>
        /// Override to get keys from the object
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        string GetLocalizationKey(params string[] param)
        {
            var key = GetType().FullName;
            return Localizable.AppendKey(key, param);
        }

        /// <summary>
        /// Get the raw content, override this for creating custom format of localized content
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        string GetRawContent(params string[] param)
        {
            var key = GetLocalizationKey(param);
            var rawString = Localization.GetContent(key, param);
            return rawString;
        }

        /// <summary>
        /// Get escape value from the object
        /// </summary>
        /// <param name="escapeKey"></param>
        /// <returns></returns>
        string GetEscapeValue(string escapeKey, params string[] param)
        {
            var value = Reflection.GetLastObject(this, escapeKey);
            if (value == null) return escapeKey;
            return value.ToString();
        }
    }

    /// <summary>
    /// Extensions for localizable interface
    /// </summary>
    public static class Localizable
    {
        public static string Localize(this ILocalizable localizable, params string[] param)
        {
            return LocalizedContent.Create(localizable).Localize(param);
        }

        public static LocalizedContent GetContent(this ILocalizable localizable)
        {
            return LocalizedContent.Create(localizable);
        }

        /// <summary>
        /// Split action parameter
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static IEnumerable<string[]> GetOpt(params string[] param)
        {
            foreach (var item in param)
            {
                int index = item.IndexOf("=");
                if (index != -1)
                {
                    yield return new string[] { item[..index], item[(index + 1)..] };
                }
                else yield return new string[] { item, null };
            }
        }

        /// <summary>
        /// append given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string AppendKey(string key, params string[] param)
        {
            var extensions = param.Where(p => !p.Contains('=') && !string.IsNullOrEmpty(p)).Prepend(key);
            return string.Join(Localization.KEY_SEPARATOR, extensions);
        }
    }
}