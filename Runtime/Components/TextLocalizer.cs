using TMPro;
using UnityEngine;

namespace Minerva.Localizations.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextLocalizer : TextLocalizerBase
    {
        public TMP_Text textField;

        void OnValidate()
        {
            textField = GetComponent<TMP_Text>();
        }

        public override void SetDisplayText(string text)
        {
            textField.text = text;
        }
    }
}
