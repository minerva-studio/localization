using UnityEngine;
using UnityEngine.UI;

namespace Minerva.Localizations.Components
{
    [RequireComponent(typeof(Text))]
    public class TextLocalizerLegacyText : TextLocalizerBase
    {
        public Text textField;

        void OnValidate()
        {
            textField = GetComponent<Text>();
        }

        public override void SetDisplayText(string text)
        {
            textField.text = text;
        }
    }
}
