using NUnit.Framework;

namespace Minerva.Localizations.Tests
{
    public class LocalizationKeyCollectionTests
    {
        [Test]
        public void Constructor_WithUniqueRows_IndexesEveryRowWithoutModifyingInput()
        {
            var rows = new[] { "ui.title", "ui.menu.start", "item.sword.name" };

            var collection = new LocalizationKeyCollection(rows);

            Assert.AreEqual(3, collection.Count);
            CollectionAssert.AreEquivalent(new[] { "ui.title", "ui.menu.start", "item.sword.name" },
                new[] { collection[0], collection[1], collection[2] });
            CollectionAssert.AreEqual(new[] { "ui.title", "ui.menu.start", "item.sword.name" }, rows);
        }
    }
}
