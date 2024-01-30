using System;
using System.Collections.Generic;

namespace Minerva.Localizations
{
    public record L10nTraverse(string Key)
    {
        public string Key { get; private set; } = Key;

        private string region;
        private List<string> options;


        private bool IsValidState => region == L10n.Region;


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
            if (options == null)
            {
                UpdateOptions();
            }
            return new(options);
        }

        private void UpdateOptions()
        {
            options = new();
            L10n.OptionOf(Key, options, true);
        }

        public bool Move(string childLevel)
        {
            if (options == null)
            {
                UpdateOptions();
            }
            return Move(options.IndexOf(childLevel));
        }

        public bool Move(int optionIndex)
        {
            if (options == null)
            {
                UpdateOptions();
            }
            if (optionIndex < 0 || optionIndex >= options.Count) return false;

            Key = Localizable.AppendKey(Key, options[optionIndex]);
            options = null;
            return true;
        }

        public bool Back()
        {
            int idx = Key.LastIndexOf(L10nSymbols.KEY_SEPARATOR);
            if (idx == -1)
            {
                Key = "";
                return false;
            }
            Key = Key[..idx];
            options = null;
            return true;
        }
    }
}