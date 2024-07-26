using System;
using System.Collections;
using System.Collections.Generic;

namespace Minerva.Localizations
{
    public record L10nTraverse
    {
        private Key key;
        private string region;
        private List<string> options;


        private bool IsValidState => region == L10n.Region;
        /// <summary>
        /// Current traverse key
        /// </summary>
        public Key Key => key;
        /// <summary>
        /// Option count
        /// </summary>
        public int Count => options?.Count ?? 0;
        public IEnumerable<string> CurrentOptions => new OptionEnumerator(this);
        public IEnumerable<string> CurrentKeys => L10n.OptionOf(key);


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

        struct OptionEnumerator : IEnumerator<string>, IEnumerable<string>
        {
            L10nTraverse self;
            int index;

            public readonly string Current => self.options[index];
            readonly object IEnumerator.Current => Current;


            public OptionEnumerator(L10nTraverse self)
            {
                this.self = self;
                this.index = -1;
                self.UpdateOptions();
            }


            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                index++;
                return self.options.Count > index;
            }

            public void Reset()
            {
                this.index = -1;
            }

            public IEnumerator<string> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}