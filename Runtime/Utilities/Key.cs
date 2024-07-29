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


        public static readonly Key Empty = new Key() { SectionArray = Array.Empty<string>() };

        public int Valid { get; private set; }
        private string CachedKeyString { get; set; }
        private string[] SectionArray { get; set; }


        public readonly string this[int index] => SectionArray[index];
        public readonly ArraySegment<string> Section => new(SectionArray, 0, Length);
        public readonly int Length => Valid;
        public readonly bool IsEmpty => Valid >= 0;


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
                        SectionArray = SectionArray,
                        Valid = length,
                    };
                }
                else
                {
                    var key = new Key
                    {
                        SectionArray = new string[length],
                        Valid = length
                    };
                    Array.Copy(SectionArray, offset, key.SectionArray, 0, length);
                    return key;
                }
            }
        }



        public Key(string key)
        {
            SectionArray = key.Split('.');
            Valid = SectionArray.Length;
            CachedKeyString = key;
            if (!SectionArray.All(k => VALID_KEY_MEMBER.IsMatch(k)))
            {
                throw new ArgumentException(key);
            }
        }

        public Key(params string[] path)
        {
            CachedKeyString = JoinString(path);
            SectionArray = CachedKeyString.Split('.');
            Valid = SectionArray.Length;
            if (!SectionArray.All(k => VALID_KEY_MEMBER.IsMatch(k)))
            {
                throw new ArgumentException(string.Join(KEY_SEPARATOR, path));
            }
        }

        public void Append(string v)
        {
            // if last member of the array is not emtpy (no write to existing) or array not long enough, just create new array
            if (SectionArray.Length == 0 || Valid == 0 || SectionArray.Length == Valid || !string.IsNullOrEmpty(SectionArray[Valid]))
            {
                // min length of 4 when append
                var newLength = Length * 2;
                newLength = newLength > 4 ? newLength : 4;

                var newArray = new string[newLength];
                Array.Copy(SectionArray, newArray, Length);
                SectionArray = newArray;
            }

            SectionArray[Valid] = v;
            Valid++;
            CachedKeyString = null;
        }




        public static Key operator +(Key a, Key b)
        {
            return Join(a, b);
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
            key.Valid -= count;
            if (key.Valid < 0)
            {
                key.Valid = 0;
            }
            key.CachedKeyString = null;
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

        public static implicit operator ArraySegment<string>(Key key)
        {
            return key.Section;
        }


        public override string ToString()
        {
            return CachedKeyString ??= string.Join(L10nSymbols.KEY_SEPARATOR, SectionArray, 0, Length);
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
            {
                yield return SectionArray[i];
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
            Append(stringBuilder, key.SectionArray);
            Append(stringBuilder, s2);
            stringBuilder.Length--;
            return stringBuilder.ToString();
        }

        public static Key Join(Key key1, Key key2)
        {
            Key result = key1;
            for (int i = 0; i < key2.Valid; i++)
            {
                result += key2[i];
            }
            return result;
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