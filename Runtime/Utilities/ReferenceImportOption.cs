using System;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Reference ($...$) import option
    /// </summary>
    [Flags]
    public enum ReferenceImportOption
    {
        Invalid = -1,
        [Tooltip("References will be replaced by its value")]
        Default = 0,
        [Tooltip("References will be replaced by its value AND add a link tag")]
        WithLinkTag = 1,
        [Tooltip("References will be replaced by its value AND add a underline")]
        WithUnderline = 2,
    }
}