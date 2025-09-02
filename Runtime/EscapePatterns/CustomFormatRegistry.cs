using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minerva.Localizations.EscapePatterns
{
    public interface IFormatHandler
    {
        bool IsHandlerFor(string format);
        string Handle<T>(T value, string format);
    }

    public static class FormatHandlerRegistry
    {
        private static readonly List<IFormatHandler> formatters = new();

        static FormatHandlerRegistry()
        {
            Register(new PermilleFormatHandler());
            Register(new SignedFormatHandler());
            Register(new BytesFormatHandler());
        }

        public static void Register(IFormatHandler handler) => formatters.Add(handler);
        public static string TryFormat(object value, string format)
        {
            foreach (var f in formatters)
            {
                if (f.IsHandlerFor(format))
                {
                    try
                    {
                        return f.Handle(value, format);
                    }
                    catch (CustomFormatterException) { }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
            // fallback 到 .NET 默认
            return value is IFormattable formattable
                ? formattable.ToString(format, null)
                : value?.ToString() ?? string.Empty;
        }


        class PermilleFormatHandler : IFormatHandler
        {
            public bool IsHandlerFor(string format)
                => format != null && format.StartsWith("permille", StringComparison.OrdinalIgnoreCase);

            public string Handle<T>(T value, string format)
            {
                if (!double.TryParse(value.ToString(), out var d))
                    return value?.ToString() ?? string.Empty;

                int decimals = 0;
                var parts = format.Split(';');
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsed))
                    decimals = parsed;

                return (d * 1000).ToString($"F{decimals}") + "‰";
            }
        }

        class SignedFormatHandler : IFormatHandler
        {
            public bool IsHandlerFor(string format)
                => string.Equals(format, "signed", StringComparison.OrdinalIgnoreCase);

            public string Handle<T>(T value, string format)
            {
                if (!double.TryParse(value.ToString(), out var d))
                    return value?.ToString() ?? string.Empty;

                return d >= 0 ? "+" + d : d.ToString();
            }
        }

        class BytesFormatHandler : IFormatHandler
        {
            private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

            public bool IsHandlerFor(string format)
                => format != null && format.StartsWith("bytes", StringComparison.OrdinalIgnoreCase);

            public string Handle<T>(T value, string format)
            {
                if (!double.TryParse(value.ToString(), out var size))
                    return value?.ToString() ?? string.Empty;

                int decimals = 1;
                var parts = format.Split(';');
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsed))
                    decimals = parsed;

                int i = 0;
                while (size >= 1024 && i < Units.Length - 1)
                {
                    size /= 1024;
                    i++;
                }
                return size.ToString($"F{decimals}") + " " + Units[i];
            }
        }
    }
}