using System.Collections.Generic;
using UnityEngine;

namespace Minerva.Localizations.Components
{
    /// <summary>
    /// Base class for custom text localizer, default implementation see <see cref="TextLocalizer"/>
    /// </summary>
    public abstract class TextLocalizerBase : MonoBehaviour
    {
        public static List<TextLocalizerBase> loaders = new List<TextLocalizerBase>();


        [Header("Key")]
        public string key;
        public L10nDataManager languageFileManager;


        void Awake()
        {
            loaders.Add(this);
        }

        void Start()
        {
            Load();
        }

        private void OnEnable()
        {
            Load();
#if UNITY_EDITOR
            if (!languageFileManager.HasKey(key) || string.IsNullOrEmpty(key)) Debug.LogError("The Language Loader does not have a valid key");
#endif
        }


        void OnDestroy()
        {
            loaders.Remove(this);
        }

        /// <summary>
        /// load current key
        /// </summary>
        public void Load()
        {
            if (string.IsNullOrEmpty(key)) return;

            var text = L10n.Tr(key);
            SetDisplayText(text);
        }

        /// <summary>
        /// load with given key
        /// </summary>
        /// <param name="newKey"></param>
        public void Load(string newKey)
        {
            this.key = newKey;
            Load();
        }

        /// <summary>
        /// Set give text as displaying text
        /// </summary>
        /// <param name="text"></param>
        public abstract void SetDisplayText(string text);

        public static void ReloadAll()
        {
            loaders.RemoveAll(x => x == null);
            foreach (var item in loaders)
            {
                if (item) item.Load();
            }
        }
    }
}