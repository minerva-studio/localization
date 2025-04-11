using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Option for underline color
    /// </summary>
    public enum UnderlineResolverOption
    {
        [Tooltip("Do nothing")]
        None,
        [Tooltip("Underline color will match content color while linking (writing tooltips)")]
        WhileLinking,
        [Tooltip("Underline color will always match content color")]
        Always,
    }
}