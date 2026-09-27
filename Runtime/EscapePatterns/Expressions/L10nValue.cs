#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;

namespace Minerva.Localizations
{
    /// <summary>
    /// A value carried through localization lookups without boxing numbers.
    /// </summary>
    public readonly struct L10nValue : IEquatable<L10nValue>
    {
        private const double LongUpperBoundExclusive = 9223372036854775808d;

        /// <summary>
        /// Describes the payload stored by a localization value.
        /// </summary>
        /// <remarks>
        /// Null is derived from the reference payload rather than stored; a destroyed Unity object therefore becomes Null.
        /// </remarks>
        public enum ValueKind : byte { Null, Number, String, Object }

        private readonly double number;
        private readonly object? reference;
        private readonly bool isNumber;

        /// <summary>
        /// Gets the payload kind.
        /// </summary>
        public ValueKind Kind
        {
            get
            {
                if (isNumber) return ValueKind.Number;
                if (reference is string) return ValueKind.String;
                return IsMissing(reference) ? ValueKind.Null : ValueKind.Object;
            }
        }

        /// <summary>
        /// Whether this value represents a missing value.
        /// </summary>
        public bool IsNull => Kind == ValueKind.Null;

        /// <summary>
        /// Gets the numeric value.
        /// </summary>
        public double Number => number;

        /// <summary>
        /// Gets the reference value.
        /// </summary>
        public object? Reference
        {
            get
            {
                ValueKind kind = Kind;
                return GetReference(kind);
            }
        }

        internal object? GetReference(ValueKind kind) => kind is ValueKind.String or ValueKind.Object ? reference : null;

        private L10nValue(bool isNumber, double number, object? reference)
        {
            this.isNumber = isNumber;
            this.number = number;
            this.reference = reference;
        }

        /// <summary>Creates a Number value.</summary>
        public static L10nValue FromNumber(double value) => new(true, value, null);
        /// <summary>Creates a value backed by the supplied string; null becomes Null.</summary>
        public static L10nValue FromString(string? value) => new(false, 0, value);
        /// <summary>Creates a tagged value, normalizing numeric primitives and booleans to Number.</summary>
        public static L10nValue FromObject(object? value)
        {
            if (IsMissing(value)) return default;
            if (value is L10nValue l10nValue) return l10nValue;
            if (value is bool boolean) return FromNumber(boolean ? 1 : 0);
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
                return FromNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            return new L10nValue(false, 0, value);
        }

        /// <summary>
        /// Converts an integer to Number without boxing.
        /// </summary>
        public static implicit operator L10nValue(int value) => FromNumber(value);
        /// <summary>
        /// Converts a long integer to Number without boxing.
        /// </summary>
        public static implicit operator L10nValue(long value) => FromNumber(value);
        /// <summary>
        /// Converts a single-precision value to Number without boxing.
        /// </summary>
        public static implicit operator L10nValue(float value) => FromNumber(value);
        /// <summary>
        /// Converts a double-precision value to Number.
        /// </summary>
        public static implicit operator L10nValue(double value) => FromNumber(value);
        /// <summary>
        /// Converts false to zero and true to one.
        /// </summary>
        public static implicit operator L10nValue(bool value) => FromNumber(value ? 1 : 0);
        /// <summary>
        /// Converts a string to String, or Null when the input is null.
        /// </summary>
        public static implicit operator L10nValue(string? value) => FromString(value);

        /// <summary>
        /// Reads supported primitive values directly and converts other values using <see cref="Convert.ChangeType(object, Type)"/>.
        /// </summary>
        public bool TryGet<T>([NotNullWhen(true)] out T? value)
        {
            ValueKind kind = Kind;
            if (kind == ValueKind.Null)
            {
                value = default;
                return false;
            }

            if (kind == ValueKind.Number)
            {
                if (typeof(T) == typeof(double))
                {
                    double v = number;
                    value = UnsafeUtility.As<double, T>(ref v)!;
                    return true;
                }
                if (typeof(T) == typeof(float))
                {
                    float v = (float)number;
                    value = UnsafeUtility.As<float, T>(ref v)!;
                    return true;
                }
                if (typeof(T) == typeof(int))
                {
                    if (IsIntegral(number) && number >= int.MinValue && number <= int.MaxValue)
                    {
                        int v = (int)number;
                        value = UnsafeUtility.As<int, T>(ref v)!;
                        return true;
                    }

                    value = default;
                    return false;
                }
                if (typeof(T) == typeof(long))
                {
                    if (IsIntegral(number) && number >= long.MinValue && number < LongUpperBoundExclusive)
                    {
                        long v = (long)number;
                        value = UnsafeUtility.As<long, T>(ref v)!;
                        return true;
                    }

                    value = default;
                    return false;
                }
                if (typeof(T) == typeof(bool))
                {
                    bool v = number != 0;
                    value = UnsafeUtility.As<bool, T>(ref v)!;
                    return true;
                }
            }

            object? boxed = kind switch
            {
                ValueKind.Number => number,
                ValueKind.String or ValueKind.Object => reference,
                _ => null
            };
            if (boxed is T typed)
            {
                value = typed;
                return true;
            }
            try
            {
                object? converted = Convert.ChangeType(boxed, typeof(T), CultureInfo.InvariantCulture);
                if (converted is null)
                {
                    value = default;
                    return false;
                }

                value = (T)converted;
                return true;
            }
            catch { value = default; return false; }
        }

        /// <summary>
        /// Returns the stored value as an object, boxing Number as a double.
        /// </summary>
        public object? ToObject()
        {
            ValueKind kind = Kind;
            return kind switch
            {
                ValueKind.Number => number,
                ValueKind.String or ValueKind.Object => reference,
                _ => null
            };
        }

        internal bool TryGetNumber(out double value)
        {
            ValueKind kind = Kind;
            return TryGetNumber(kind, out value);
        }

        internal bool TryGetNumber(ValueKind kind, out double value)
        {
            if (kind == ValueKind.Number) { value = number; return true; }
            if (kind == ValueKind.String) return double.TryParse((string)reference!, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            value = 0;
            return false;
        }

        public override string ToString()
        {
            ValueKind kind = Kind;
            if (kind == ValueKind.Null) return string.Empty;
            if (kind == ValueKind.Number)
            {
                float asFloat = (float)number;
                return (double)asFloat == number
                    ? asFloat.ToString(CultureInfo.InvariantCulture)
                    : number.ToString(CultureInfo.InvariantCulture);
            }
            return kind == ValueKind.String ? (string)reference! : reference?.ToString() ?? string.Empty;
        }

        public bool Equals(L10nValue other)
        {
            ValueKind kind = Kind;
            ValueKind otherKind = other.Kind;
            if (kind != otherKind) return false;
            if (kind == ValueKind.Null) return true;
            return kind == ValueKind.Number ? number.Equals(other.number) : Equals(reference, other.reference);
        }

        public override bool Equals(object? obj) => obj is L10nValue other && Equals(other);
        public override int GetHashCode()
        {
            ValueKind kind = Kind;
            if (kind == ValueKind.Null) return (int)ValueKind.Null;
            return kind == ValueKind.Number ? HashCode.Combine(kind, number) : HashCode.Combine(kind, reference);
        }

        public static bool operator ==(L10nValue left, L10nValue right) => left.Equals(right);
        public static bool operator !=(L10nValue left, L10nValue right) => !left.Equals(right);

        private static bool IsIntegral(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && Math.Truncate(value) == value;

        public static bool IsMissing(object? value) => value is null || value is UnityEngine.Object unityObject && unityObject == null;
    }
}
