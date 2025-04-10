using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Reference ($...$) import option
    /// </summary>
    public enum ReferenceImportOption
    {
        [Tooltip("References will be replaced by its value")]
        Default,
        [Tooltip("References will be replaced by its value AND add a link tag")]
        WithLinkTag,
    }
}