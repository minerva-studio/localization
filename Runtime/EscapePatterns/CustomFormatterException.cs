using System;

namespace Minerva.Localizations.EscapePatterns
{
    public class CustomFormatterException : Exception
    {
        public CustomFormatterException() { }
        public CustomFormatterException(string message) : base(message) { }
        public CustomFormatterException(string message, Exception inner) : base(message, inner) { }
        protected CustomFormatterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}