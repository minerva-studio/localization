using System;

namespace Minerva.Localizations.Utilities
{
    public interface IReflectionProxy
    {
        /// <summary>
        /// Try get the value from the path, if true then the value is valid
        /// </summary>
        /// <param name="target"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGet(object target, ReadOnlySpan<char> path, out object value);
    }
}
