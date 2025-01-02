﻿using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Solution to key not found when trying to read a localization dictionary
    /// </summary>
    public enum MissingKeySolution
    {
        [Tooltip("Display the raw key, ie \"Lozalization.LocalizationFile.name\"")]
        RawDisplay,
        [Tooltip("Display an empty string")]
        Empty,
        [Tooltip("Force a content being display on the screen, usually the last part of the key")]
        ForceDisplay,
        [Tooltip("Fallback to other language")]
        Fallback,
    }
}