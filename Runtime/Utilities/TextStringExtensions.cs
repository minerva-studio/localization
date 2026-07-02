using System.Text;
using UnityEngine;

namespace Minerva.Localizations.Utilities
{
    public static class TextStringExtensions
    {
        public static string UGUIColor(this string str, string colorCode)
        {
            return $"<color={colorCode}>{str}</color>";
        }

        public static string UGUIColor(this string str, Color color)
        {
            string colorHex = color.a == 1 ? ColorUtility.ToHtmlStringRGB(color) : ColorUtility.ToHtmlStringRGBA(color);
            return str.UGUIColor($"#{colorHex}");
        }

        public static string ToTitleCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }
            if (text.Length < 2)
            {
                return text.ToUpper();
            }

            StringBuilder sb = new();
            sb.Append(char.ToUpper(text[0]));
            bool isCapitalized = true;

            for (int i = 1; i < text.Length; i++)
            {
                bool isCurrentCapitalized = char.IsUpper(text, i);
                if (isCurrentCapitalized && !isCapitalized)
                {
                    sb.Append(' ');
                    sb.Append(char.ToUpper(text[i]));
                }
                else
                {
                    sb.Append(text[i]);
                }
                isCapitalized = isCurrentCapitalized;
            }
            return sb.ToString();
        }
    }
}
