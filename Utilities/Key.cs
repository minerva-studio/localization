using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Minerva.Localizations.L10nSymbols;

namespace Minerva.Localizations
{
    /// <summary>
    /// Localization key in construction
    /// </summary>
    public struct Key : IEnumerable<string>
    {
        public static readonly Regex VALID_KEY_MEMBER = new(@"^[A-Za-z0-9_]+$");
        public static readonly Regex VALID_KEY = new(@"(?:([A-Za-z0-9_-]+)\.?)+");


        public static readonly Key Empty = new Key() { levels = Array.Empty<string>() };

        public int valid { get; private set; }
        private string[] levels { get; set; }
        private string cachedKeyString { get; set; }

        public string this[int index]
        {
            get => levels[index];
            set => levels[index] = value;
        }

        public Key this[Range range]
        {
            get
            {
                var (offset, length) = range.GetOffsetAndLength(Length);
                if (offset == 0)
                {
                    if (length > this.Length) throw new IndexOutOfRangeException();
                    return new Key()
                    {
                        levels = levels,
                        valid = length,
                    };
                }
                else
                {
                    var key = new Key
                    {
                        levels = new string[length],
                        valid = length
                    };
                    Array.Copy(levels, offset, key.levels, 0, length);
                    return key;
                }
            }
        }

        public int Length => valid;


        public Key(string key)
        {
            levels = key.Split('.');
            valid = levels.Length;
            cachedKeyString = key;
            if (!levels.All(k => VALID_KEY_MEMBER.IsMatch(k)))
            {
                throw new ArgumentException(key);
            }
        }

        public Key(params string[] path)
        {
            cachedKeyString = string.Join(KEY_SEPARATOR, path);
            levels = cachedKeyString.Split('.');
            valid = levels.Length;
            if (!levels.All(k => VALID_KEY_MEMBER.IsMatch(k)))
            {
                throw new ArgumentException(string.Join(KEY_SEPARATOR, path));
            }
        }

        public void Append(string v)
        {
            var newArray = new string[Length + 1];
            Array.Copy(levels, newArray, Length);
            newArray[^1] = v;
            levels = newArray;
            valid++;
            cachedKeyString = null;
        }




        public static Key operator +(Key key, string next)
        {
            var ret = key;
            ret.Append(next);
            return ret;
        }

        public static Key operator -(Key key, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            key.valid -= count;
            if (key.valid < 0)
            {
                key.valid = 0;
            }
            key.cachedKeyString = null;
            return key;
        }

        public static implicit operator string(Key key)
        {
            return key.ToString();
        }

        public static implicit operator Key(string key)
        {
            return new Key(key);
        }


        public override string ToString()
        {
            return cachedKeyString ??= string.Join(L10nSymbols.KEY_SEPARATOR, levels, 0, Length);
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
            {
                yield return levels[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}