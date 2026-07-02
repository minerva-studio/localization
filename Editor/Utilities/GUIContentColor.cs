using System;
using UnityEngine;

namespace Minerva.Localizations.Editor.Utilities
{
    /// <summary>
    /// Temporarily overrides GUI.contentColor and restores the previous state on dispose.
    /// </summary>
    public readonly struct GUIContentColor : IDisposable
    {
        private readonly Color lastState;

        public GUIContentColor(Color color)
        {
            lastState = GUI.contentColor;
            GUI.contentColor = color;
        }

        public static GUIContentColor By(Color value)
        {
            return new GUIContentColor(value);
        }

        public void Dispose()
        {
            GUI.contentColor = lastState;
        }
    }
}
