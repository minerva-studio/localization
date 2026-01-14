using System;
using System.Collections.Generic;
using System.Text;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// Severity level of an L10n evaluation error
    /// </summary>
    public enum L10nErrorSeverity
    {
        Warning,  // Non-critical, fallback provided
        Error,    // Critical, evaluation failed but fallback provided
        Fatal,    // Unrecoverable error
    }

    /// <summary>
    /// Represents a single error that occurred during L10n evaluation
    /// </summary>
    public class L10nEvaluationError
    {
        public L10nErrorSeverity Severity { get; }
        public string TokenContext { get; }      // The token that caused the error
        public string ErrorType { get; }         // e.g., "ParseError", "VariableNotFound", "RecursionDepth"
        public string Message { get; }
        public Exception InnerException { get; } // Original exception, if any 

        public L10nEvaluationError(
            L10nErrorSeverity severity,
            string tokenContext,
            string errorType,
            string message,
            Exception innerException = null)
        {
            Severity = severity;
            TokenContext = tokenContext;
            ErrorType = errorType;
            Message = message;
            InnerException = innerException;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Severity}] {ErrorType}: {Message}");
            if (!string.IsNullOrEmpty(TokenContext))
                sb.Append($" (...{TokenContext}...)");
            if (InnerException != null)
                sb.Append($" → {InnerException.GetType().Name}: {InnerException.Message}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Aggregates all errors and diagnostics from an L10n evaluation session
    /// </summary>
    public class L10nEvaluationDiagnostics
    {
        private readonly List<L10nEvaluationError> errors = new List<L10nEvaluationError>();
        public IReadOnlyList<L10nEvaluationError> Errors => errors.AsReadOnly();

        public bool HasErrors => errors.Count > 0;
        public bool HasFatalErrors => errors.Exists(e => e.Severity == L10nErrorSeverity.Fatal);

        public void AddError(L10nEvaluationError error)
        {
            errors.Add(error);
        }

        public void AddError(
            L10nErrorSeverity severity,
            string tokenContext,
            string errorType,
            string message,
            Exception innerException = null)
        {
            errors.Add(new L10nEvaluationError(severity, tokenContext, errorType, message, innerException));
        }

        public void Clear()
        {
            errors.Clear();
        }

        /// <summary>
        /// Get a human-readable summary of all errors
        /// </summary>
        public string GetSummary()
        {
            if (errors.Count == 0)
                return "No errors";

            var summary = new StringBuilder();
            summary.AppendLine($"L10n Evaluation Diagnostics ({errors.Count} issues):");

            int warningCount = 0, errorCount = 0, fatalCount = 0;

            foreach (var err in errors)
            {
                switch (err.Severity)
                {
                    case L10nErrorSeverity.Warning: warningCount++; break;
                    case L10nErrorSeverity.Error: errorCount++; break;
                    case L10nErrorSeverity.Fatal: fatalCount++; break;
                }

                summary.AppendLine($"- {err}");
            }

            summary.AppendLine();
            summary.AppendLine($"Summary: {warningCount} warnings, {errorCount} errors, {fatalCount} fatal");
            return summary.ToString();
        }

        /// <summary>
        /// Get errors by severity level
        /// </summary>
        public IReadOnlyList<L10nEvaluationError> GetErrorsBySeverity(L10nErrorSeverity severity)
        {
            return errors.FindAll(e => e.Severity == severity).AsReadOnly();
        }

        public L10nEvaluationDiagnostics Clone()
        {
            var clone = new L10nEvaluationDiagnostics();
            foreach (var error in errors)
            {
                clone.AddError(error);
            }
            return clone;
        }


        public static implicit operator bool(L10nEvaluationDiagnostics diagnostics)
        {
            return diagnostics != null && diagnostics.HasErrors;
        }
    }
}