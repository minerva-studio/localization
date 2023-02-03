using System.Collections.Generic;
using System.Linq;

namespace Amlos.Localizations
{

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