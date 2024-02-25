using System;
using System.Collections.Generic;

namespace Minerva.Localizations
{
    public record L10nTraverse
    {
        private Key key;
        private string region;
        private List<string> options;


        private bool IsValidState => region == L10n.Region;

        public L10nTraverse(Key key)
        {
            this.key = key;
        }

        public L10nTraverse(string str) : this(new Key(str)) { }
        public L10nTraverse() : this("")
        {
            L10n.OnLocalizationLoaded += L10n_OnLocalizationLoaded;
            region = L10n.Region;
        }

        private void L10n_OnLocalizationLoaded()
        {
            // clear options
            options = null;
            region = L10n.Region;
        }


        /// <summary>
        /// Get all options (possible complete key) of the partial key
        /// </summary>
        /// <param name="partialKey"></param>
        /// <param name="firstLevelOnly">Whether returning full key or next class only</param>
        /// <returns></returns>
        public List<string> GetOptions()
        {
            if (L10n.Instance == null) throw new InvalidOperationException();
            options ??= UpdateOptions();
            if (options == null) return new();
            return new(options);
        }

        private List<string> UpdateOptions()
        {
            options = new();
            if (!L10n.OptionOf(key, options, true))
            {
                options = null;
            }
            return options;
        }

        public bool Move(string childLevel)
        {
            options ??= UpdateOptions();
            if (options == null)
            {
                return false;
            }
            return Move(options.IndexOf(childLevel));
        }

        public bool Move(int optionIndex)
        {
            options ??= UpdateOptions();
            if (options == null) return false;
            if (optionIndex < 0 || optionIndex >= options.Count) return false;

            key.Append(options[optionIndex]);
            options = null;
            return true;
        }

        public bool Back()
        {
            key -= 1;
            options = null;
            return true;
        }
    }
}