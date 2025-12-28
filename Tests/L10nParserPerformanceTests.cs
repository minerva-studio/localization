using Minerva.Localizations.EscapePatterns;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.PerformanceTesting;
using Debug = UnityEngine.Debug;

namespace Minerva.Localizations.Tests
{
    [TestFixture]
    public class L10nParserPerformanceTests
    {
        private TestL10nContext testContext;

        [SetUp]
        public void Setup()
        {
            testContext = new TestL10nContext();
        }

        #region Test Cases

        private static readonly string[] SimpleTestCases = new[]
        {
            "Hello World",
            "Hello {playerName}!",
            "You have {coins} coins",
            "§RRed Text§",
            "$common.button.ok$",
        };

        private static readonly string[] ComplexTestCases = new[]
        {
            "§GPlayer §Y{playerName}§G has §C{gold}§G gold and §B{level}§G levels§",
            "Quest: $quest.main.title$ - Reward: {reward:F0} coins",
            "{damage * multiplier:F2} damage dealt to {targetName}",
            "§#FF5733Complex §#33FF57nested §#3357FFcolor§ tags§§",
            "$item.weapon.sword$ deals {baseDamage + bonusDamage:F1} damage",
            "Status: {health / maxHealth * 100:F0}% HP",
            "§RCritical Hit!§ You dealt {critDamage:F0} damage to $@enemy.boss.name$",
            "{Math.Max(minDamage, actualDamage):F0} final damage",
        };

        private static readonly string[] EdgeCaseTestCases = new[]
        {
            @"Escaped \$ and \{ and \§ symbols",
            "Nested: {outerVar<innerVar>}",
            "Multiple: {a} {b} {c} {d} {e}",
            "$key.with.dots$ and $another.key$",
            "§R§G§B§Y§W§",
        };

        #endregion 

        #region Simple Cases

        [Test, Performance]
        public void Simple_NewParser()
        {
            using (new LegacyParserScope(false))
            {
                Measure.Method(() =>
                {
                    foreach (var testCase in SimpleTestCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(5)
                .GC()
                .Run();
            }
        }

        [Test, Performance]
        public void Simple_LegacyParser()
        {
            using (new LegacyParserScope(true))
            {
                Measure.Method(() =>
                {
                    foreach (var testCase in SimpleTestCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(5)
                .GC()
                .Run();
            }
        }

        #endregion

        #region Complex Cases

        [Test, Performance]
        public void Complex_NewParser()
        {
            using (new LegacyParserScope(false))
            {
                Measure.Method(() =>
                {
                    foreach (var testCase in ComplexTestCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(3)
                .GC()
                .Run();
            }
        }

        [Test, Performance]
        public void Complex_LegacyParser()
        {
            using (new LegacyParserScope(true))
            {
                Measure.Method(() =>
                {
                    foreach (var testCase in ComplexTestCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(3)
                .GC()
                .Run();
            }
        }

        #endregion

        #region Edge Cases

        [Test, Performance]
        public void EdgeCases_NewParser()
        {
            using (new LegacyParserScope(false))
            {
                Measure.Method(() =>
                {
                    foreach (var testCase in EdgeCaseTestCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(5)
                .GC()
                .Run();
            }
        }

        [Test, Performance]
        public void EdgeCases_LegacyParser()
        {
            using (new LegacyParserScope(true))
            {
                Measure.Method(() =>
                {
                    foreach (var testCase in EdgeCaseTestCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(5)
                .GC()
                .Run();
            }
        }

        #endregion 

        #region Manual Benchmarking

        [Explicit, Test]
        public void ManualBenchmark_Comparison()
        {
            const int iterations = 1000;
            var testCases = new List<string>();
            testCases.AddRange(SimpleTestCases);
            testCases.AddRange(ComplexTestCases);
            testCases.AddRange(EdgeCaseTestCases);

            // Warmup - 减少预热次数
            using (new LegacyParserScope(false))
            {
                foreach (var testCase in SimpleTestCases) // 只预热简单用例
                {
                    EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                }
            }
            using (new LegacyParserScope(true))
            {
                foreach (var testCase in SimpleTestCases)
                {
                    EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                }
            }

            // Benchmark New Parser
            var sw = Stopwatch.StartNew();
            using (new LegacyParserScope(false))
            {
                for (int i = 0; i < iterations; i++)
                {
                    foreach (var testCase in testCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                }
            }
            sw.Stop();
            var newParserTime = sw.Elapsed.TotalMilliseconds;

            // Benchmark Legacy Parser
            sw.Restart();
            using (new LegacyParserScope(true))
            {
                for (int i = 0; i < iterations; i++)
                {
                    foreach (var testCase in testCases)
                    {
                        EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
                    }
                }
            }
            sw.Stop();
            var legacyParserTime = sw.Elapsed.TotalMilliseconds;

            var speedup = legacyParserTime / newParserTime;

            Debug.Log($"=== Performance Comparison ({iterations} iterations) ===");
            Debug.Log($"New Parser:    {newParserTime:F2} ms ({newParserTime / iterations:F4} ms/iter)");
            Debug.Log($"Legacy Parser: {legacyParserTime:F2} ms ({legacyParserTime / iterations:F4} ms/iter)");
            Debug.Log($"Speedup:       {speedup:F2}x");
            Debug.Log($"Improvement:   {(speedup > 1 ? (speedup - 1) * 100 : -(1 / speedup - 1) * 100):F1}%");

            Assert.Pass($"Speedup: {speedup:F2}x");
        }

        [Explicit, Test]
        public void QuickBenchmark()
        {
            const int iterations = 100;
            var testCase = ComplexTestCases[0];

            // Warmup
            using (new LegacyParserScope(false))
                EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
            using (new LegacyParserScope(true))
                EscapePattern.Escape(testCase, testContext, L10nParams.Empty);

            // Benchmark
            var sw = Stopwatch.StartNew();
            using (new LegacyParserScope(false))
            {
                for (int i = 0; i < iterations; i++)
                    EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
            }
            sw.Stop();
            var newTime = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            using (new LegacyParserScope(true))
            {
                for (int i = 0; i < iterations; i++)
                    EscapePattern.Escape(testCase, testContext, L10nParams.Empty);
            }
            sw.Stop();
            var legacyTime = sw.Elapsed.TotalMilliseconds;

            Debug.Log($"Quick Benchmark ({iterations} iterations):");
            Debug.Log($"  New: {newTime:F2} ms | Legacy: {legacyTime:F2} ms | Speedup: {legacyTime / newTime:F2}x");

            Assert.Pass();
        }

        #endregion

        #region Test Context

        private class TestL10nContext : ILocalizableContext
        {
            private readonly Dictionary<string, object> variables = new Dictionary<string, object>
            {
                { "playerName", "TestPlayer" },
                { "coins", 1234 },
                { "gold", 5678 },
                { "level", 42 },
                { "damage", 100.5 },
                { "multiplier", 1.5 },
                { "targetName", "Enemy" },
                { "baseDamage", 50 },
                { "bonusDamage", 25.5 },
                { "health", 750 },
                { "maxHealth", 1000 },
                { "critDamage", 999 },
                { "reward", 500.0 },
                { "minDamage", 10 },
                { "actualDamage", 75 },
                { "a", "A" },
                { "b", "B" },
                { "c", "C" },
                { "d", "D" },
                { "e", "E" },
                { "outerVar", "outer" },
                { "innerVar", "inner" },
            };

            public object GetEscapeValue(string escapeKey, L10nParams param)
            {
                if (variables.TryGetValue(escapeKey, out var value))
                {
                    return value;
                }

                return $"[{escapeKey}]";
            }

            public Key GetLocalizationKey(L10nParams param)
            {
                return new Key("Test", "Context");
            }

            public string GetRawContent(L10nParams param)
            {
                return "TestContent";
            }
        }

        #endregion
    }
}