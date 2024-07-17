using System;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// A entry in localization system
    /// </summary>
    [Serializable]
    internal class Entry : IEquatable<Entry>, IComparable<Entry>
    {
        [SerializeField] private string key;
        [SerializeField] private string value;


        public string Value { get => value; set => this.value = value; }
        public string Key { get => key; set => key = value; }


        public Entry(string key, string value)
        {
            Key = key;
            Value = value;
        }


        public bool Equals(Entry other)
        {
            return other.value == value && other.key == key;
        }

        public override bool Equals(object obj)
        {
            return obj is Entry entry && Equals(entry);
        }

        public int CompareTo(Entry other)
        {
            return key.CompareTo(other.key);
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }
    }
}