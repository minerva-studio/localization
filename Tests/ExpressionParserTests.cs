using Minerva.Localizations.EscapePatterns;
using Minerva.Localizations.Utilities;
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

            public override bool TryGetEscapeValue(string escapeKey, L10nParams parameters, out L10nValue value)
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

            public override bool TryGetEscapeValue(string escapeKey, L10nParams parameters, out L10nValue value)
            {
                if (escapeKey == "Items") { value = L10nValue.FromObject(items); return true; }
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
        [Test]
        public void PlainText_ShouldBypassTemplateCache()
        {
            string source = "plain text " + Guid.NewGuid().ToString("N");
            bool wasLegacy = EscapePattern.UseLegacyParser;
            EscapePattern.UseLegacyParser = false;
            try
            {
                int entriesBefore = L10nTemplate.CachedEntryCount;

                Assert.That(L10n.TrRaw(source, null, L10nParams.Empty), Is.EqualTo(source));
                Assert.That(L10nTemplate.CachedEntryCount, Is.EqualTo(entriesBefore));
            }
            finally { EscapePattern.UseLegacyParser = wasLegacy; }
        }

        [Test]
        public void BackslashOnlyText_ShouldUseTokenizer()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string source = @"escaped\text-" + suffix;
            bool wasLegacy = EscapePattern.UseLegacyParser;
            EscapePattern.UseLegacyParser = false;
            try
            {
                Assert.That(L10n.TrRaw(source, null, L10nParams.Empty), Is.EqualTo("escapedtext-" + suffix));
            }
            finally { EscapePattern.UseLegacyParser = wasLegacy; }
        }

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
        public void DynamicIndexKeyInterning_ShouldStopGrowingAndKeepResolving()
        {
            var items = new int[512];
            for (int i = 0; i < items.Length; i++) items[i] = i;

            var expression = L10nExpressionCompiler.Compile("Items[index]");
            var evaluator = L10nEvaluator.Rent(new EvaluationContext(null, L10nParams.Empty));
            try
            {
                for (int i = 0; i < items.Length; i++)
                {
                    var parameters = L10nParams.Empty.With("Items", items).With("index", i);
                    evaluator.Reset(new EvaluationContext(null, parameters));
                    Assert.That(evaluator.EvaluateExpression(expression).TryGet<int>(out var value), Is.True);
                    Assert.That(value, Is.EqualTo(i));
                }

                Assert.That(expression.Paths[0].InternedKeyNodeCount, Is.LessThanOrEqualTo(L10nPath.MaximumInternedKeyNodesPerPath));
            }
            finally { L10nEvaluator.Return(evaluator); }
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
            var template = L10nTemplate.GetOrCompile("{Names[index + 1]}");
            var arithmeticExpression = L10nExpressionCompiler.Compile("x * 2 + 1");
            var output = L10nObjectPool.RentStringBuilder();
            try
            {
                evaluator.Evaluate(template, output);
                Assert.That(output.ToString(), Is.EqualTo("one"));
                output.Clear();
                Assert.That(evaluator.EvaluateExpression(arithmeticExpression).TryGet<double>(out var number), Is.True);
                Assert.That(number, Is.EqualTo(7));
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

        private sealed class NumberDisplayContext : L10nContext
        {
            public NumberDisplayContext(object value) : base(value) { }
        }

        private sealed class NumericFields
        {
            public int Integer = 3;
            public float Single = 3f;
            public float Rounded = 2.26f;
        }

        [Test]
        public void NumberDisplay_ShouldUseCompactDefaultAndExplicitFormat()
        {
            var context = new NumberDisplayContext(new NumericFields());
            Assert.That(L10n.TrRaw("{Integer}", context, L10nParams.Empty), Is.EqualTo("3"));
            Assert.That(L10n.TrRaw("{Single}", context, L10nParams.Empty), Is.EqualTo("3"));
            Assert.That(L10n.TrRaw("{Rounded}", context, L10nParams.Empty), Is.EqualTo("2.3"));
            Assert.That(L10n.TrRaw("{level+1}", context, L10nParams.Empty.With("level", 3)), Is.EqualTo("4"));
            Assert.That(L10n.TrRaw("{x:F1}", context, L10nParams.Empty.With("x", 3)), Is.EqualTo("3.0"));
        }

        [Test]
        public void BooleanParameter_ShouldRoundTripAsNumber()
        {
            var parameters = L10nParams.Empty.With("Generic", true);
            Assert.That(parameters.TryGetVariable("Generic", out bool value), Is.True);
            Assert.That(value, Is.True);
            Assert.That(L10nValue.FromNumber(2.5).TryGet<int>(out _), Is.False);
            Assert.That(L10nValue.FromObject((short)3).TryGet<double>(out var normalized), Is.True);
            Assert.That(normalized, Is.EqualTo(3));
        }

        private sealed class StateWithIndex
        {
            public int Index { get; set; } = 4;
        }

        private sealed class FloatOverrideContext : L10nContext
        {
            public override bool TryGetEscapeValue(string escapeKey, L10nParams parameters, out L10nValue value)
            {
                if (escapeKey == "Value") { value = 2.5f; return true; }
                value = default;
                return false;
            }
        }

        [Test]
        public void TypedPropertyGetterAndContextOverride_ShouldNotBoxDuringExpressionEvaluation()
        {
            var parameters = L10nParams.Empty.With("State", new StateWithIndex());
            var evaluator = L10nEvaluator.Rent(new EvaluationContext(null, parameters));
            var propertyExpression = L10nExpressionCompiler.Compile("State.Index * 2");
            try
            {
                Assert.That(evaluator.EvaluateExpression(propertyExpression).TryGet<double>(out var result), Is.True);
                Assert.That(result, Is.EqualTo(8));
                Assert.That(() => { _ = evaluator.EvaluateExpression(propertyExpression); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(NUnit.Framework.Is.Not));
            }
            finally { L10nEvaluator.Return(evaluator); }

            evaluator = L10nEvaluator.Rent(new EvaluationContext(new FloatOverrideContext(), L10nParams.Empty));
            var overrideExpression = L10nExpressionCompiler.Compile("Value * 2");
            try
            {
                Assert.That(evaluator.EvaluateExpression(overrideExpression).TryGet<double>(out var result), Is.True);
                Assert.That(result, Is.EqualTo(5));
                Assert.That(() => { _ = evaluator.EvaluateExpression(overrideExpression); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(NUnit.Framework.Is.Not));
            }
            finally { L10nEvaluator.Return(evaluator); }
        }
    }

    public class BoundedConcurrentCacheTests
    {
        [Test]
        public void KeyStringCache_ShouldRemainBoundedAndRebuildEvictedKeys()
        {
            var cache = KeyStringCache.Shared;
            cache.Clear();

            try
            {
                const int capacity = KeyStringCache.MaximumEntries;
                for (int i = 0; i <= capacity; i++)
                    cache.GetString(new Key($"BoundedCacheKey{i}"));

                Assert.That(cache.Count, Is.LessThanOrEqualTo(capacity));
                Assert.That(cache.GetString(new Key("BoundedCacheKey0")), Is.EqualTo("BoundedCacheKey0"));
                Assert.That(cache.Count, Is.LessThanOrEqualTo(capacity));
            }
            finally { cache.Clear(); }
        }

        [Test]
        public void AddingPastCapacity_ShouldEvictOldestEntryIndividually()
        {
            var cache = new BoundedConcurrentCache<int, string>(2);
            cache.GetOrAdd(1, "one");
            cache.GetOrAdd(2, "two");
            cache.GetOrAdd(3, "three");

            Assert.That(cache.Count, Is.EqualTo(2));
            Assert.That(cache.TryGetValue(1, out _), Is.False);
            Assert.That(cache.TryGetValue(2, out var second), Is.True);
            Assert.That(second, Is.EqualTo("two"));
            Assert.That(cache.TryGetValue(3, out var third), Is.True);
            Assert.That(third, Is.EqualTo("three"));
        }

        [Test]
        public void ConcurrentInsertions_ShouldStayWithinCapacity()
        {
            var cache = new BoundedConcurrentCache<int, int>(16);
            System.Threading.Tasks.Parallel.For(0, 1024, value => cache.GetOrAdd(value, value));

            Assert.That(cache.Count, Is.LessThanOrEqualTo(16));
        }

        [Test]
        public void Clear_ShouldRemoveEntriesAndAllowFurtherInsertions()
        {
            var cache = new BoundedConcurrentCache<int, string>(2);
            cache.GetOrAdd(1, "one");
            cache.GetOrAdd(2, "two");

            cache.Clear();

            Assert.That(cache.Count, Is.Zero);
            Assert.That(cache.TryGetValue(1, out _), Is.False);
            Assert.That(cache.GetOrAdd(3, "three"), Is.EqualTo("three"));
            Assert.That(cache.Count, Is.EqualTo(1));
        }
    }

    public class L10nValueTests
    {
        private sealed class TestAsset : UnityEngine.ScriptableObject { }

        private sealed class Holder
        {
            public UnityEngine.Object Target { get; set; }
        }

        [Test]
        public void TryGet_ShouldReturnFalseForNullValue()
        {
            Assert.That(default(L10nValue).TryGet<string>(out var value), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(L10nValue.FromString(null).TryGet<object>(out _), Is.False);
        }

        [Test]
        public void DestroyedUnityObject_ShouldBecomeNullAndEqualDefault()
        {
            TestAsset asset = UnityEngine.ScriptableObject.CreateInstance<TestAsset>();
            try
            {
                L10nValue value = L10nValue.FromObject(asset);
                Assert.That(value.Kind, Is.EqualTo(L10nValue.ValueKind.Object));

                UnityEngine.Object.DestroyImmediate(asset);
                asset = null;

                Assert.That(value.IsNull, Is.True);
                Assert.That(value.Kind, Is.EqualTo(L10nValue.ValueKind.Null));
                Assert.That(value.ToObject(), Is.Null);
                Assert.That(value.Reference, Is.Null);
                Assert.That(value, Is.EqualTo(default(L10nValue)));
            }
            finally
            {
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void DestroyedUnityObjectInParameterPath_ShouldFallBackToCanonicalKey()
        {
            TestAsset asset = UnityEngine.ScriptableObject.CreateInstance<TestAsset>();
            try
            {
                var parameters = L10nParams.Empty.With("Holder", new Holder { Target = asset });
                UnityEngine.Object.DestroyImmediate(asset);
                asset = null;

                Assert.That(L10n.TrRaw("{Holder.Target.name}", L10nContext.None(), parameters), Is.EqualTo("Holder.Target.name"));
            }
            finally
            {
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CorruptedPayloadCombinations_ShouldHaveDeterministicValueKind()
        {
            var type = typeof(L10nValue);
            var numberTag = type.GetField("isNumber", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var numberField = type.GetField("number", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var referenceField = type.GetField("reference", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(numberTag, Is.Not.Null);
            Assert.That(numberField, Is.Not.Null);
            Assert.That(referenceField, Is.Not.Null);

            object missingPayload = default(L10nValue);
            numberTag.SetValue(missingPayload, false);
            referenceField.SetValue(missingPayload, null);
            L10nValue missing = (L10nValue)missingPayload;
            Assert.That(missing, Is.EqualTo(default(L10nValue)));
            Assert.That(missing.IsNull, Is.True);

            object numberWithReference = default(L10nValue);
            numberTag.SetValue(numberWithReference, true);
            numberField.SetValue(numberWithReference, 12.5d);
            referenceField.SetValue(numberWithReference, "ignored");
            L10nValue number = (L10nValue)numberWithReference;
            Assert.That(number.Kind, Is.EqualTo(L10nValue.ValueKind.Number));
            Assert.That(number.Reference, Is.Null);
            Assert.That(number.TryGet<double>(out var value), Is.True);
            Assert.That(value, Is.EqualTo(12.5));
        }
    }
}
