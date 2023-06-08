using UnityEngine;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Color code used in localization
    /// </summary>
    public static class ColorCode
    {
        /// <summary>
        /// Get color by single color char
        /// </summary>
        /// <param name="colorCode"></param>
        /// <returns></returns>
        public static Color GetColor(char colorCode)
        {
            /**
            b: blue
            g: green
            r: red
            c: cyan
            m: magenta
            y: yellow
            k: black
            w: white
            */
            switch (colorCode)
            {
                case 'B':
                case 'b':
                    return Color.blue;
                case 'G':
                case 'g':
                    return Color.green;
                case 'R':
                case 'r':
                    return Color.red;
                case 'C':
                case 'c':
                    return Color.cyan;
                case 'M':
                case 'm':
                    return Color.magenta;
                case 'Y':
                case 'y':
                    return Color.yellow;
                case 'K':
                case 'k':
                    return Color.black;
                case 'W':
                case 'w':
                    return Color.white;
                default:
                    break;
            }
            return Color.white;
        }

        /// <summary>
        /// Get color hex code
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string GetColorHex(Color color)
        {
            return $"#{(int)(color.r * 255f):X2}{(int)(color.g * 255f):X2}{(int)(color.b * 255f):X2}{(int)(color.a * 255f):X2}";
        }

        /// <summary>
        /// Get color hex code
        /// </summary>
        /// <param name="colorCode"></param>
        /// <returns></returns>
        public static string GetColorHex(char colorCode)
        {
            return GetColorHex(GetColor(colorCode));
        }
    }
}