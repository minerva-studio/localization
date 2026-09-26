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

    public class LocalizationIndexExpressionTests
    {
        private class TestContext : L10nContext
        {
            public TestContext(object value) : base(value) { }
        }

        private sealed class OverrideContext : TestContext
        {
            public OverrideContext(object value) : base(value) { }

            public override bool TryGetEscapeValue(string escapeKey, L10nParams parameters, out object value)
            {
                if (escapeKey == "Items[0].Name") { value = "override"; return true; }
                return base.TryGetEscapeValue(escapeKey, parameters, out value);
            }
        }

        private sealed class RootPrefixContext : TestContext
        {
            private readonly Entry[] items;

            public RootPrefixContext(Entry[] items) : base(null)
            {
                this.items = items;
            }

            public override bool TryGetEscapeValue(string escapeKey, L10nParams parameters, out object value)
            {
                if (escapeKey == "Items") { value = items; return true; }
                return base.TryGetEscapeValue(escapeKey, parameters, out value);
            }
        }

        private sealed class Entry
        {
            public string Name { get; set; }
        }

        private sealed class Group
        {
            public Entry[] Items { get; set; }
        }

        private sealed class State
        {
            public int Index { get; set; }
        }

        private sealed class Root
        {
            public Entry[] Items { get; set; }
            public System.Collections.Generic.List<Entry> EntryList { get; set; }
            public int[][] Matrix { get; set; }
            public Group[] Groups { get; set; }
            public State State { get; set; }
        }

        private sealed class Aliased
        {
            [L10nReferAs("rate")] public int SpeedRate { get; set; }
            public Aliased Child { get; set; }
        }

        private static Root CreateRoot() => new()
        {
            Items = new[] { new Entry { Name = "zero" }, new Entry { Name = "one" } },
            EntryList = new System.Collections.Generic.List<Entry> { new Entry { Name = "list" } },
            Matrix = new[] { new[] { 1, 2 }, new[] { 8, 9 } },
            Groups = new[] { new Group { Items = new[] { new Entry { Name = "nested" } } } },
            State = new State { Index = 0 }
        };

        [Test]
        public void BaseValue_ShouldResolveIndexThenMemberAndNestedPaths()
        {
            var context = new TestContext(CreateRoot());

            Assert.That(L10n.TrRaw("{Items[0].Name}", context, L10nParams.Empty), Is.EqualTo("zero"));
            Assert.That(L10n.TrRaw("{Groups[State.Index].Items[0].Name}", context, L10nParams.Empty), Is.EqualTo("nested"));
            Assert.That(L10n.TrRaw("{Matrix[1][1]}", context, L10nParams.Empty), Is.EqualTo("9"));
            Assert.That(L10n.TrRaw("{EntryList[0].Name}", context, L10nParams.Empty), Is.EqualTo("list"));
        }

        [Test]
        public void L10nReferAsAlias_ShouldResolveRootAndNestedMembers()
        {
            var context = new TestContext(new Aliased { SpeedRate = 3, Child = new Aliased { SpeedRate = 7 } });

            Assert.That(L10n.TrRaw("{rate}", context, L10nParams.Empty), Is.EqualTo("3"));
            Assert.That(L10n.TrRaw("{Child.rate}", context, L10nParams.Empty), Is.EqualTo("7"));
        }

        [Test]
        public void Parameters_ShouldResolveArithmeticIndexAndObjectRoot()
        {
            var context = new TestContext(null);
            var parameters = L10nParams.Empty
                .With("Items", CreateRoot().Items)
                .With("index", 0);

            Assert.That(L10n.TrRaw("{Items[index + 1].Name}", context, parameters), Is.EqualTo("one"));

            var nestedParameters = L10nParams.Empty
                .With("Groups", CreateRoot().Groups)
                .With("State", new State { Index = 0 });
            Assert.That(L10n.TrRaw("{Groups[State.Index].Items[0].Name}", context, nestedParameters), Is.EqualTo("nested"));
        }

        [Test]
        public void FullParameterKey_ShouldTakePrecedenceOverDynamicContext()
        {
            var context = new DynamicContext(CreateRoot());
            context["Items[0].Name"] = "context";
            var parameters = L10nParams.Empty.With("Items[0].Name", "parameter");

            Assert.That(L10n.TrRaw("{Items[0].Name}", context, parameters), Is.EqualTo("parameter"));
        }

        [Test]
        public void DynamicContext_ShouldResolveCurrentAndParentFullPathOverrides()
        {
            var parent = new DynamicContext(CreateRoot());
            parent["Items[0].Name"] = "parent";
            var current = new DynamicContext(parent);
            current["Items[0].Name"] = "current";

            Assert.That(L10n.TrRaw("{Items[0].Name}", current, L10nParams.Empty), Is.EqualTo("current"));

            current.dynamicValues.Remove("Items[0].Name");
            Assert.That(L10n.TrRaw("{Items[0].Name}", current, L10nParams.Empty), Is.EqualTo("parent"));
        }

        [Test]
        public void LocalAndGlobalProviders_ShouldReceiveCompleteResolvedPath()
        {
            const string key = "Items[0].Name";
            var context = new TestContext(CreateRoot());
            var hadGlobal = L10nContext.GlobalEscapeValue.TryGetValue(key, out var previousGlobal);

            try
            {
                context.LocalEscapeValue[key] = (name, _) => "local";
                Assert.That(L10n.TrRaw("{Items[0].Name}", context, L10nParams.Empty), Is.EqualTo("local"));

                context.LocalEscapeValue.Remove(key);
                L10nContext.GlobalEscapeValue[key] = (name, _) => "global";
                Assert.That(L10n.TrRaw("{Items[0].Name}", context, L10nParams.Empty), Is.EqualTo("global"));
            }
            finally
            {
                context.LocalEscapeValue.Remove(key);
                if (hadGlobal) L10nContext.GlobalEscapeValue[key] = previousGlobal;
                else L10nContext.GlobalEscapeValue.Remove(key);
            }
        }

        [Test]
        public void CustomGetEscapeValueOverride_ShouldReceiveCompleteResolvedPath()
        {
            var context = new OverrideContext(CreateRoot());

            Assert.That(L10n.TrRaw("{Items[0].Name}", context, L10nParams.Empty), Is.EqualTo("override"));
        }

        [Test]
        public void ContextRootValue_ShouldBeWalkedWhenCompletePathIsNotResolved()
        {
            var context = new RootPrefixContext(new[]
            {
                new Entry { Name = "zero" },
                new Entry { Name = "one" }
            });

            Assert.That(L10n.TrRaw("{Items[1].Name}", context, L10nParams.Empty), Is.EqualTo("one"));
        }

        [Test]
        public void ParameterPrefix_ShouldTakePrecedenceOverContextPath()
        {
            var context = new DynamicContext(null);
            context["Items[0].Name"] = "context";
            var parameters = L10nParams.Empty.With("Items", new[] { new Entry { Name = "parameter" } });

            Assert.That(L10n.TrRaw("{Items[0].Name}", context, parameters), Is.EqualTo("parameter"));
        }

        [Test]
        public void PathArguments_ShouldReachProviderLookup()
        {
            var context = new TestContext(null);
            L10nParams received = default;
            context.LocalEscapeValue["Items[0].Name"] = (_, parameters) =>
            {
                received = parameters;
                return "value";
            };

            Assert.That(L10n.TrRaw("{Items[0].Name<opt>}", context, L10nParams.Empty), Is.EqualTo("value"));
            Assert.That(received.Options, Is.EqualTo(new[] { "opt" }));
        }

        [Test]
        public void MissingNullAndOutOfRangePaths_ShouldUseExistingKeyFallback()
        {
            var root = CreateRoot();
            root.Items[0] = null;
            var context = new TestContext(root);

            Assert.That(context.GetEscapeValue("Items[0].Name", L10nParams.Empty), Is.EqualTo("Items[0].Name"));
            Assert.That(context.GetEscapeValue("Items[99].Name", L10nParams.Empty), Is.EqualTo("Items[99].Name"));
            Assert.That(context.GetEscapeValue("State[0].Index", L10nParams.Empty), Is.EqualTo("State[0].Index"));
            Assert.That(context.GetEscapeValue("Items[0].Missing", L10nParams.Empty), Is.EqualTo("Items[0].Missing"));
        }

    }

    public class CompiledLocalizationExpressionTests
    {
        [TestCase("Items[0].Name()")]
        [TestCase("Items[0].Name trailing")]
        [TestCase("Items[0.Name")]
        [TestCase("(Items[0].Name")]
        [TestCase("Items[0.5].Name")]
        [TestCase("Items[2147483648].Name")]
        public void Compiler_ShouldStoreSyntaxErrorAndPosition(string source)
        {
            var expression = L10nExpressionCompiler.Compile(source);

            Assert.That(expression.Error, Is.Not.Null.And.Contains("position"));
        }

        [Test]
        public void DynamicIndexPath_ShouldResolveMoreThanFourIndices()
        {
            object nested = "leaf";
            for (int i = 0; i < 5; i++) nested = new object[] { nested };
            var context = L10nContext.None();
            var parameters = L10nParams.Empty.With(
                ("M", nested), ("a", 0), ("b", 0), ("c", 0), ("d", 0), ("e", 0));

            Assert.That(L10n.TrRaw("{M[a][b][c][d][e]}", context, parameters), Is.EqualTo("leaf"));
        }

        [Test]
        public void DynamicIndexArithmetic_ShouldProduceCanonicalPrefixKey()
        {
            var context = L10nContext.None();
            var parameters = L10nParams.Empty.With("Items", new[] { "zero", "one", "two" }).With("index", 1);

            Assert.That(L10n.TrRaw("{Items[index + 1]}", context, parameters), Is.EqualTo("two"));
        }

        [Test]
        public void NonIntegerDynamicIndex_ShouldEmitOriginalExpression()
        {
            var context = L10nContext.None();
            var parameters = L10nParams.Empty.With("Items", new[] { "zero" }).With("index", 0);

            Assert.That(L10n.TrRaw("{Items[index + 0.5]}", context, parameters), Is.EqualTo("Items[index + 0.5]"));
        }

        [Test]
        public void CachedExpressionEvaluation_ShouldNotAllocateAfterWarmup()
        {
            var names = new[] { "zero", "one", "two" };
            var parameters = L10nParams.Empty.With("Names", names).With("index", 0).With("x", 3);
            var evaluatorContext = new EvaluationContext(null, parameters);
            var evaluator = L10nEvaluator.Rent(evaluatorContext);
            var template = L10nTemplateCache.Get("{Names[index + 1]}");
            var arithmeticExpression = L10nExpressionCompiler.Compile("x * 2 + 1");
            var output = L10nObjectPool.RentStringBuilder();
            try
            {
                evaluator.Evaluate(template, output);
                Assert.That(output.ToString(), Is.EqualTo("one"));
                output.Clear();
                Assert.That(evaluator.EvaluateExpression(arithmeticExpression).Number, Is.EqualTo(7));
                Assert.That(() =>
                {
                    output.Clear();
                    evaluator.Evaluate(template, output);
                    _ = evaluator.EvaluateExpression(arithmeticExpression);
                }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(NUnit.Framework.Is.Not));
            }
            finally
            {
                L10nObjectPool.ReturnStringBuilder(output);
                L10nEvaluator.Return(evaluator);
            }
        }
    }
}
