using Minerva.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrayUtility = Minerva.Module.ArrayUtility;

namespace Minerva.Localizations
{
#if UNITY_EDITOR
    [Serializable]
    public class LocalizationKeyCollection : IEnumerable<string>, ICollection<string>
    {
        public Trie keyTrie;
        string[] indexed;

        public ICollection<string> FirstLevelKeys => keyTrie.FirstLayerKeys;
        public int Count => keyTrie.Count;
        public bool IsReadOnly => ((ICollection<string>)indexed).IsReadOnly;

        public string this[int index]
        {
            get
            {
                if (indexed?.Length != keyTrie.Count) indexed = null;
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

        public async Task UnionWithAsync(IEnumerable<string> keys)
        {
            await Task.Run(() =>
            {
                UnionWith(keys);
                indexed = keyTrie.ToArray();
            });
        }

        public bool TryGetSegment(string pKey, out TrieSegment subTrie)
        {
            return keyTrie.TryGetSegment(pKey, out subTrie);
        }

        public IEnumerator<string> GetEnumerator()
        {
            indexed ??= keyTrie.ToArray();
            return ((IEnumerable<string>)indexed).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            indexed ??= keyTrie.ToArray();
            return indexed.GetEnumerator();
        }

        public void Clear()
        {
            indexed = null;
            keyTrie.Clear(true);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            indexed.CopyTo(array, arrayIndex);
        }
    }
#endif
}
