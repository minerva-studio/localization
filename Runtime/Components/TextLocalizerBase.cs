using Minerva.Module.Editor;
using System.Collections.Generic;
using UnityEngine;

namespace Minerva.Localizations.Components
{
    /// <summary>
    /// Base class for custom text localizer, default implementation see <see cref="TextLocalizer"/>
    /// </summary>
    public abstract class TextLocalizerBase : MonoBehaviour
    {
        public static List<TextLocalizerBase> loaders;


        [Header("Key")]
        public string key;
        public ILocalizableContext context;
        public L10nDataManager languageFileManager;



        static TextLocalizerBase()
        {
            loaders = new();
            L10n.OnLocalizationLoaded += ReloadAll;
        }

        private void OnValidate()
        {
            LogMissingMessage();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color color;
            if (string.IsNullOrEmpty(key))
            {
                color = Color.red;
            }
            else if (languageFileManager.HasKey(key))
            {
                color = Color.white;
            }
            else if (languageFileManager.IsInSource(key))
            {
                color = Color.yellow;
            }
            else
            {
                color = Color.red;
            }
            using (new GizmosColor(color))
            {
                Vector3 size;
                if (transform is RectTransform rectTransform)
                {
                    Vector3[] corners = new Vector3[4];
                    rectTransform.GetWorldCorners(corners);
                    float width = Vector3.Distance(corners[0], corners[3]);
                    float height = Vector3.Distance(corners[0], corners[1]);
                    size = new Vector3(width, height, 0);
                }
                else
                {
                    size = Vector3.one;
                }
                Gizmos.DrawWireCube(transform.position, size);
            }
        }
#endif

        private void LogMissingMessage()
        {
#if UNITY_EDITOR 
            if (string.IsNullOrEmpty(key)) Debug.LogWarning($"Key is missing for {name}", this);
#endif
        }

        void Awake()
        {
            loaders.Add(this);
            LogMissingMessage();
        }

        void Start()
        {
            Load();
        }

        private void OnEnable()
        {
            Load();
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
            if (!enabled) return;
            if (string.IsNullOrEmpty(key)) return;
            string text = context != null ? L10n.TrKey(key, context) : L10n.Tr(key);
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