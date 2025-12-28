using Minerva.Localizations.EscapePatterns;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Minerva.Localizations.Tests
{
    /// <summary>
    /// Unity EditMode NUnit tests for ExpressionParser.
    /// </summary>
    public class ExpressionParserTests
    {
        // --- Helpers ---------------------------------------------------------

        /// <summary>
        /// Parse and evaluate the expression, returning the raw object.
        /// </summary>
        private static object EvalAny(string expr, IDictionary<string, object> vars)
        {
            var parser = new ExpressionParser.Parser(expr);
            var node = parser.ParseExpression();

            return node.Run(mem =>
            {
                var key = mem.ToString();
                if (vars != null && vars.TryGetValue(key, out var v))
                    return v;
                throw new KeyNotFoundException($"Variable '{key}' not provided.");
            });
        }

        /// <summary>
        /// Evaluate as float (InvariantCulture).
        /// </summary>
        private static float EvalFloat(string expr, IDictionary<string, object> vars = null)
        {
            var value = EvalAny(expr, vars);
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Evaluate as string (throws if result is not string).
        /// </summary>
        private static string EvalString(string expr, IDictionary<string, object> vars = null)
        {
            var value = EvalAny(expr, vars);
            if (value is string s) return s;
            throw new InvalidCastException($"Result is not a string (actual: {value?.GetType().Name ?? "null"}).");
        }

        // --- Tests -----------------------------------------------------------

        [TestCase(0f, 0f)]
        [TestCase(1f, 0.5f)]
        [TestCase(3f, 0.75f)]
        [TestCase(0.25f, 0.2f)] // 1 - 1 / 1.25 = 0.2
        public void ComplexExpression_OneMinusReciprocal_ShouldEvaluateCorrectly(float increase, float expected)
        {
            const string expr = "1-(1/(increase+1))";
            var vars = new Dictionary<string, object> { { "increase", increase } };

            var value = EvalFloat(expr, vars);
            Assert.That(value, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void OperatorPrecedence_ShouldRespect_MulDivOverAddSub()
        {
            Assert.That(EvalFloat("2+3*4"), Is.EqualTo(14f).Within(1e-4f));
            Assert.That(EvalFloat("(2+3)*4"), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(EvalFloat("10-6/3"), Is.EqualTo(8f).Within(1e-4f));
        }

        [Test]
        public void UnaryMinus_NumberLiteral_ShouldWork()
        {
            Assert.That(EvalFloat("-0.5+1"), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(EvalFloat("1-(-2)"), Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void Variables_StringMultiplyAndConcat_ShouldReturnString()
        {
            var vars = new Dictionary<string, object>
            {
                { "a", "ha" },
                { "b", "!" },
                { "n", 3f }
            };

            // a * n => "hahaha"
            var s1 = EvalString("a*n", vars);
            Assert.That(s1, Is.EqualTo("hahaha"));

            // a*n + b => "hahaha!"
            var s2 = EvalString("a*n+b", vars);
            Assert.That(s2, Is.EqualTo("hahaha!"));
        }

        [Test]
        public void Variables_NumberArithmetic_ShouldUseProvidedVars()
        {
            var vars = new Dictionary<string, object> { { "x", 2f }, { "y", 5f } };
            Assert.That(EvalFloat("x+y*3", vars), Is.EqualTo(17f).Within(1e-4f));
            Assert.That(EvalFloat("(x+y)^2", vars), Is.EqualTo(49f).Within(1e-4f)); // if '^' is power
        }

        [Test]
        public void MissingVariable_ShouldThrow()
        {
            Assert.Throws<KeyNotFoundException>(() => EvalAny("x+1", new Dictionary<string, object>()));
        }
    }
}
