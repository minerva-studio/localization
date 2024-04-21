using Minerva.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Minerva.Localizations
{
#if UNITY_EDITOR
    [Serializable]
    public class LocalizationKeyCollection : IEnumerable<string>
    {
        public Trie keyTrie;
        string[] indexed;

        public ICollection<string> FirstLevelKeys => keyTrie.FirstLevelKeys;

        public string this[int index]
        {
            get
            {
                indexed ??= keyTrie.ToArray();
                return indexed[index];
            }
        }



        public LocalizationKeyCollection(string[] rows)
        {
            keyTrie = new Trie(rows);
            indexed = new string[rows.Length];
            Array.Copy(indexed, rows, indexed.Length);
        }

        public LocalizationKeyCollection()
        {
            keyTrie ??= new Trie();
            indexed = null;
        }

        public void Add(string key)
        {
            keyTrie.Add(key);
            if (indexed != null) ArrayUtility.Add(ref indexed, key);
        }

        public bool Remove(string key)
        {
            bool v = keyTrie.Remove(key);
            if (v && indexed != null) ArrayUtility.Remove(ref indexed, key);
            return v;
        }

        public bool Contains(string key)
        {
            return keyTrie.Contains(key);
        }

        public void UnionWith(IEnumerable<string> keys)
        {
            keyTrie.AddRange(keys);
            indexed = null;
        }

        public bool TryGetSubTrie(string pKey, out Trie subTrie)
        {
            return keyTrie.TryGetSubTrie(pKey, out subTrie);
        }

        public IEnumerator<string> GetEnumerator()
        {
            indexed ??= keyTrie.ToArray();
            foreach (var item in indexed)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            indexed ??= keyTrie.ToArray();
            return indexed.GetEnumerator();
        }
    }
#endif
}
