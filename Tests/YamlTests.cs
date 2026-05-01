using NUnit.Framework;
using System.Collections.Generic;

namespace Minerva.Localizations.Tests
{
    public class YamlTests
    {
        [Test]
        public void Import_ReadsPlainLocalizationText()
        {
            const string yaml =
                "UI:\n" +
                "  Greeting: \"Hello, world!\"\n" +
                "  Tooltip: \"Press A: confirm # not a comment\"\n" +
                "  RawQuote: He said \"hello\" today\n" +
                "  Contraction: It's safe\n";

            var result = Yaml.Import(yaml);

            Assert.That(result["UI.Greeting"], Is.EqualTo("Hello, world!"));
            Assert.That(result["UI.Tooltip"], Is.EqualTo("Press A: confirm # not a comment"));
            Assert.That(result["UI.RawQuote"], Is.EqualTo("He said \"hello\" today"));
            Assert.That(result["UI.Contraction"], Is.EqualTo("It's safe"));
        }

        [Test]
        public void Import_ReadsEscapedDoubleQuotedText()
        {
            const string yaml =
                "Dialogue:\n" +
                "  Line: \"She said \\\"Library\\\" and left.\"\n" +
                "  Path: \"Assets\\\\Localizations\\\\en.yml\"\n" +
                "  Multiline: \"First line\\nSecond line\"\n";

            var result = Yaml.Import(yaml);

            Assert.That(result["Dialogue.Line"], Is.EqualTo("She said \"Library\" and left."));
            Assert.That(result["Dialogue.Path"], Is.EqualTo(@"Assets\Localizations\en.yml"));
            Assert.That(result["Dialogue.Multiline"], Is.EqualTo("First line\nSecond line"));
        }

        [Test]
        public void Export_RoundTripsLocalizationTextWithQuotesAndEscapes()
        {
            var source = new Dictionary<string, string>
            {
                ["Dialogue.Line"] = "She said \"Library\" and left.",
                ["Dialogue.Path"] = @"Assets\Localizations\en.yml",
                ["Dialogue.Multiline"] = "First line\nSecond line",
                ["UI.Tooltip"] = "Press A: confirm # not a comment",
            };

            string exported = Yaml.Export(source);
            var imported = Yaml.Import(exported);

            Assert.That(imported, Is.EquivalentTo(source));
        }

        [Test]
        public void Export_EscapesDoubleQuotedYamlStringContent()
        {
            var source = new Dictionary<string, string>
            {
                ["Quote"] = "A \"quoted\" word",
                ["Slash"] = @"A\B",
                ["LineBreak"] = "A\nB",
            };

            string exported = Yaml.ExportFullKey(source);

            Assert.That(exported, Does.Contain("Quote: \"A \\\"quoted\\\" word\""));
            Assert.That(exported, Does.Contain("Slash: \"A\\\\B\""));
            Assert.That(exported, Does.Contain("LineBreak: \"A\\nB\""));
        }
    }
}
