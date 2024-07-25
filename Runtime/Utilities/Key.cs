using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Minerva.Localizations.L10nSymbols;

namespace Minerva.Localizations
{
    /// <summary>
    /// Localization key in construction
    /// </summary>
    public struct Key : IEnumerable<string>
    {
        public static readonly Regex VALID_KEY_MEMBER = new(@"^[A-Za-z0-9_\-+]+$");
        public static readonly Regex VALID_KEY = new(@"(?:([A-Za-z0-9_-]+)\.?)+");


        public static readonly Key Empty = new Key() { levels = Array.Empty<string>() };

        public int valid { get; private set; }
        public string[] levels { get; private set; }
        private string cachedKeyString { get; set; }

        public readonly string this[int index] => levels[index];

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
            cachedKeyString = JoinString(path);
            levels = cachedKeyString.Split('.');
            valid = levels.Length;
            if (!levels.All(k => VALID_KEY_MEMBER.IsMatch(k)))
            {
                throw new ArgumentException(string.Join(KEY_SEPARATOR, path));
            }
        }

        public void Append(string v)
        {
            // if last member of the array is not emtpy (no write to existing) or array not long enough, just create new array
            if (levels.Length == 0 || valid == 0 || levels.Length == valid || !string.IsNullOrEmpty(levels[valid - 1]))
            {
                // min length of 4 when append
                var newLength = Length * 2;
                newLength = newLength > 4 ? newLength : 4;

                var newArray = new string[newLength];
                Array.Copy(levels, newArray, Length);
                levels = newArray;
            }

            levels[valid] = v;
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

        public static implicit operator Key(string[] key)
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

        public static string JoinString(params string[] s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Append(stringBuilder, s);
            stringBuilder.Length--;
            return stringBuilder.ToString();
        }

        public static string JoinString(string[] s, params string[] s2)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Append(stringBuilder, s);
            Append(stringBuilder, s2);
            stringBuilder.Length--;
            return stringBuilder.ToString();
        }

        public static string JoinString(Key key, params string[] s2)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Append(stringBuilder, key.levels);
            Append(stringBuilder, s2);
            stringBuilder.Length--;
            return stringBuilder.ToString();
        }

        private static void Append(StringBuilder stringBuilder, string[] s)
        {
            foreach (var item in s)
            {
                stringBuilder.Append(item);
                if (stringBuilder[^1] != KEY_SEPARATOR)
                {
                    stringBuilder.Append(KEY_SEPARATOR);
                }
            }
        }
    }
}