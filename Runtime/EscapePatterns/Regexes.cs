using System.Text.RegularExpressions;

namespace Minerva.Localizations.EscapePatterns
{
    public static class Regexes
    {
        public static readonly Regex UNDERLINE_TAG = new(@"<u>(.*?)</u>");
        public static readonly Regex COLOR_TAG = new(@"(<color\s*?=\s*?.*?>)(.*?)(</color>)");
        public static readonly Regex DYNAMIC_VALUE_ARG_PATTERN = new(@"(?<!\\)(?:\\{2})*(\{([\w.]*?)(?:<(?:([\w.~=]+),?)*)>?\})");
        public static readonly Regex DYNAMIC_EXPR_BRACKET_ARG_PATTERN = new(@"(?<!\\)(?:\\{2})*(\{(.+?)(?::(.+?))?\})");
        public static readonly Regex DYNAMIC_ARG_PATTERN = new(@"([a-zA-Z\._0-9]+)(?:<(?:([\w.~=]+),?)*>)?");
        public static readonly Regex CONTENT_REFERENCE_PATTERN = new(@"(?<!\\)(?:\\{2})*(\$(?:@)?([\w.]*?)(?:<(?:([\w.~=]+),?)*>)?\$)");
        public static readonly Regex COLOR_SIMPLE_PATTERN = new(@"(?<!\\)(?:\\{2})*§(.)([\s\S]*?)§");
        public static readonly Regex COLOR_CODE_PATTERN = new(@"(?<!\\)(?:\\{2})*§(#[0-9A-Fa-f]{6})([\s\S]*?)§");
        public static readonly Regex KEYWORD_COLOR_PATTERN = new(@"(?<!\\)(?:\\{2})*§<([A-Z][A-Za-z]+)>([\s\S]*?)§");
        public static readonly Regex BACKSLASH_PATTERN = new(@"\\(.)");
    }
}