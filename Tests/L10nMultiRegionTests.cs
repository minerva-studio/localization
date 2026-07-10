using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Minerva.Localizations.Tests
{
    /// <summary>
    /// Tests the multi-region runtime without touching project localization assets.
    /// </summary>
    public class L10nMultiRegionTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            L10n.DeInitialize();
            foreach (var obj in createdObjects)
            {
                if (obj)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void Load_KeepsFallbackOutOfMainAndLoadsBackgroundRegions()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            L10n.Load("Default");
            Assert.That(L10n.HasMainRegion, Is.False);
            Assert.That(L10n.Region, Is.Empty);

            L10n.Load("EN-US");
            Assert.That(L10n.Region, Is.EqualTo("EN-US"));

            L10n.Load("ZH-CN");
            Assert.That(L10n.Region, Is.EqualTo("EN-US"));
            Assert.That(L10n.LoadedRegions, Does.Contain("ZH-CN"));

            L10n.Load("ZH-CN", asMainRegion: true);
            Assert.That(L10n.Region, Is.EqualTo("ZH-CN"));
        }

        /// <summary>
        /// Verifies that the public static facade delegates main-region operations to the shared runtime.
        /// </summary>
        [Test]
        public void StaticFacade_DelegatesMainRegionOperationsToRuntime()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            Assert.That(L10n.Manager, Is.SameAs(manager));
            Assert.That(L10n.IsInitialized, Is.True);
            Assert.That(L10n.IsLoaded, Is.False);

            L10n.Load("EN-US", asMainRegion: true);
            Assert.That(L10n.Contains("Term"), Is.True);
            Assert.That(L10n.Contains("FallbackOnly"), Is.False);
            Assert.That(L10n.Exist("FallbackOnly"), Is.True);
            Assert.That(L10n.OptionOf("Group"), Is.EquivalentTo(new[] { "One", "Two" }));

            var copiedOptions = new List<string>();
            Assert.That(L10n.CopyOptions("Group", copiedOptions), Is.True);
            Assert.That(copiedOptions, Is.EquivalentTo(new[] { "One", "Two" }));
            Assert.That(L10n.Write("Term", "Changed"), Is.True);
            Assert.That(L10n.Tr("Term"), Is.EqualTo("Changed"));
        }

        /// <summary>
        /// Verifies that deinitialization clears the runtime and main-region-derived formatting state.
        /// </summary>
        [Test]
        public void DeInitialize_ClearsRuntimeAndMainRegionSettings()
        {
            var manager = CreateManager();

            L10n.InitAndLoad(manager, "EN-US");
            Assert.That(L10n.WordSpace, Is.EqualTo(" "));
            Assert.That(L10n.ListDelimiter, Is.EqualTo(", "));

            L10n.DeInitialize();

            Assert.That(L10n.Manager, Is.Null);
            Assert.That(L10n.IsInitialized, Is.False);
            Assert.That(L10n.IsLoaded, Is.False);
            Assert.That(L10n.Region, Is.Empty);
            Assert.That(L10n.LoadedRegions, Is.Empty);
            Assert.That(L10n.WordSpace, Is.Empty);
            Assert.That(L10n.ListDelimiter, Is.Empty);
        }

        [Test]
        public void ForRegion_ResolvesReferencesWithinExplicitRegion()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            L10n.Load("EN-US", asMainRegion: true);
            L10n.Load("ZH-CN");

            Assert.That(L10n.Tr("Sentence"), Is.EqualTo("Apple"));
            Assert.That(L10n.ForRegion("ZH-CN").Tr("Sentence"), Is.EqualTo("ZhApple"));
            Assert.That(L10n.Region, Is.EqualTo("EN-US"));
        }

        [Test]
        public void InRegion_ResolvesContextRawContentWithoutChangingMainRegion()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            L10n.Load("EN-US", asMainRegion: true);
            L10n.Load("ZH-CN");

            // Use a context instead of a bare key so callers do not need to duplicate key-building rules.
            var context = L10nContext.Of("ScopedSentence");
            var parameters = L10nParams.Create();

            using (L10n.InRegion("ZH-CN"))
            {
                // Context remains the raw-content source; the scope only redirects static key lookups.
                string rawContent = context.GetRawContent(parameters);
                Assert.That(rawContent, Is.EqualTo("ZH $Term$"));
                Assert.That(L10n.TrRaw(rawContent, context, parameters), Is.EqualTo("ZH ZhApple"));
            }

            Assert.That(context.GetRawContent(parameters), Is.EqualTo("EN $Term$"));
            Assert.That(L10n.TrRaw("EN $Term$", context, parameters), Is.EqualTo("EN Apple"));
            Assert.That(L10n.TrIn("ZH-CN", context, parameters), Is.EqualTo("ZH ZhApple"));
            Assert.That(L10n.TrRawIn("ZH-CN", "ZH $Term$", context, parameters), Is.EqualTo("ZH ZhApple"));
            Assert.That(L10n.TryTrIn("ZH-CN", context, parameters).TranslatedText, Is.EqualTo("ZH ZhApple"));
            Assert.That(L10n.TryTrRawIn("ZH-CN", "ZH $Term$", context, parameters).TranslatedText, Is.EqualTo("ZH ZhApple"));
            Assert.That(L10n.Region, Is.EqualTo("EN-US"));
        }

        [Test]
        public void InRegion_NestedScopesRestorePreviousRegion()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            L10n.Load("EN-US", asMainRegion: true);
            L10n.Load("ZH-CN");

            using (L10n.InRegion("ZH-CN"))
            {
                Assert.That(L10n.Tr("Sentence"), Is.EqualTo("ZhApple"));

                using (L10n.InRegion("EN-US"))
                {
                    // The inner scope temporarily replaces only the current async-local region context.
                    Assert.That(L10n.Tr("Sentence"), Is.EqualTo("Apple"));
                }

                Assert.That(L10n.Tr("Sentence"), Is.EqualTo("ZhApple"));
            }

            Assert.That(L10n.Tr("Sentence"), Is.EqualTo("Apple"));
            Assert.That(L10n.Region, Is.EqualTo("EN-US"));
        }

        [Test]
        public void InRegion_DisposeIsIdempotent()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            L10n.Load("EN-US", asMainRegion: true);
            L10n.Load("ZH-CN");

            var outerScope = L10n.InRegion("ZH-CN");
            var innerScope = L10n.InRegion("EN-US");

            innerScope.Dispose();
            Assert.That(L10n.Tr("Sentence"), Is.EqualTo("ZhApple"));

            outerScope.Dispose();
            Assert.That(L10n.Tr("Sentence"), Is.EqualTo("Apple"));

            // A repeated inner dispose must not restore the already-ended outer region.
            innerScope.Dispose();
            Assert.That(L10n.Tr("Sentence"), Is.EqualTo("Apple"));
        }

        [Test]
        public void Unload_RejectsFallbackAndMainRegions()
        {
            var manager = CreateManager();

            L10n.Init(manager);
            L10n.Load("EN-US", asMainRegion: true);
            L10n.Load("ZH-CN");

            Assert.That(L10n.Unload("Default"), Is.False);
            Assert.That(L10n.Unload("EN-US"), Is.False);
            Assert.That(L10n.Unload("ZH-CN"), Is.True);
            Assert.That(L10n.IsRegionLoaded("Default"), Is.True);
            Assert.That(L10n.IsRegionLoaded("ZH-CN"), Is.False);
        }

        /// <summary>
        /// Creates an in-memory manager with fallback and two real regions.
        /// </summary>
        private L10nDataManager CreateManager()
        {
            var manager = ScriptableObject.CreateInstance<L10nDataManager>();
            createdObjects.Add(manager);

            var fallback = CreateLanguageFile("Default", new()
            {
                ["FallbackOnly"] = "Fallback",
                ["Sentence"] = "$Term$",
                ["ScopedSentence"] = "Fallback $Term$",
                ["Term"] = "FallbackTerm",
            });
            var en = CreateLanguageFile("EN-US", new()
            {
                ["Sentence"] = "$Term$",
                ["ScopedSentence"] = "EN $Term$",
                ["Term"] = "Apple",
                ["Group.One"] = "One",
                ["Group.Two"] = "Two",
            });
            en.listDelimiter = ", ";
            en.wordSpace = " ";
            var zh = CreateLanguageFile("ZH-CN", new()
            {
                ["Sentence"] = "$Term$",
                ["ScopedSentence"] = "ZH $Term$",
                ["Term"] = "ZhApple",
            });

            manager.defaultRegion = "Default";
            manager.files = new List<LanguageFile> { fallback, en, zh };
            manager.sources = new List<LanguageFileSource>();
            manager.regions = new List<string> { "Default", "EN-US", "ZH-CN" };
            manager.missingKeySolution = MissingKeySolution.RawDisplay;
            return manager;
        }

        /// <summary>
        /// Creates an in-memory master language file for a test region.
        /// </summary>
        private LanguageFile CreateLanguageFile(string region, Dictionary<string, string> entries)
        {
            var file = ScriptableObject.CreateInstance<LanguageFile>();
            createdObjects.Add(file);

            var serializedObject = new UnityEditor.SerializedObject(file);
            serializedObject.FindProperty("region").stringValue = region;
            serializedObject.FindProperty(LanguageFile.IS_MASTER_FILE_NAME).boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            file.ImportFromDictiontary(entries);
            return file;
        }
    }
}
