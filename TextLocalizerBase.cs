using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Amlos.Localizations
{
    /// <summary>
    /// Base class for custom text localizer, default implementation see <see cref="TextLocalizer"/>
    /// </summary>
    public abstract class TextLocalizerBase : MonoBehaviour
    {
        public static List<TextLocalizerBase> loaders = new List<TextLocalizerBase>();
        public LocalizationDataManager languageFileManager;
        public TMP_Text textField;

        [Header("Key")]
        [SerializeField] private string key;
        private LocalizedContent localizedContent;

        public string Key { get => key; set { key = value; LocalizedContent = LocalizedContent.Create(value); } }
        private LocalizedContent LocalizedContent { get => localizedContent ??= LocalizedContent.Create(key); set => localizedContent = value; }




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
        }

        void OnDestroy()
        {
            loaders.Remove(this);
        }




        public void Load()
        {
            if (textField)
            {
                textField.text = LocalizedContent.Localize();
            }
        }

        public static void ReloadAll()
        {
            foreach (var item in loaders)
            {
                if (item) item.Load();
            }
        }
    }
}