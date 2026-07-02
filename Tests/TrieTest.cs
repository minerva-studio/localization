using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva.Localizations.Collections;

namespace Minerva.Localizations.Collections.Tests
{
    public class TrieTests
    {
        [Test]
        public void Trie_Add_Contains_Remove_Works()
        {
            var trie = new Trie();
            Assert.IsTrue(trie.Add("a.b.c"));
            Assert.IsTrue(trie.Contains("a.b.c"));
            Assert.IsFalse(trie.Add("a.b.c"), "Duplicate add should return false");
            Assert.IsTrue(trie.ContainsPartialKey("a.b"));
            Assert.IsTrue(trie.Remove("a.b.c"));
            Assert.IsFalse(trie.Contains("a.b.c"));
            Assert.IsFalse(trie.ContainsPartialKey("a.b"), "After removing only key, partial should be false");
        }

        [Test]
        public void Trie_Set_TogglesPresence()
        {
            var trie = new Trie();
            Assert.IsTrue(trie.Set("x.y", true));
            Assert.IsTrue(trie.Contains("x.y"));
            Assert.IsTrue(trie.Set("x.y", false));
            Assert.IsFalse(trie.Contains("x.y"));
        }

        [Test]
        public void Trie_Add_With_ListPrefix()
        {
            var trie = new Trie();
            var path = new List<string> { "room", "kitchen", "sink" };
            Assert.IsTrue(trie.Add(path));
            Assert.IsTrue(trie.Contains("room.kitchen.sink"));
            Assert.IsTrue(trie.Contains(path));
        }

        [Test]
        public void Trie_Keys_And_FirstLayerKeys()
        {
            var trie = new Trie();
            trie.Add("a.b");
            trie.Add("a.c");
            trie.Add("x.y");
            var keys = ((IEnumerable<string>)trie.Keys).ToArray();
            CollectionAssert.AreEquivalent(new[] { "a.b", "a.c", "x.y" }, keys);

            var firstLayer = ((IEnumerable<string>)trie.FirstLayerKeys).ToArray();
            // The first layer should only contain the top-level path segments.
            CollectionAssert.AreEquivalent(new[] { "a", "x" }, firstLayer);
        }

        [Test]
        public void Trie_GetSubTrie_And_TryGetSubTrie()
        {
            var trie = new Trie();
            trie.Add("a.b");
            trie.Add("a.c");
            trie.Add("x.y");

            var aSub = trie.GetSubTrie("a");
            CollectionAssert.AreEquivalent(new[] { "b", "c" }, ((IEnumerable<string>)aSub.FirstLayerKeys).ToArray());

            Assert.IsTrue(trie.TryGetSubTrie("x", out var xSub));
            CollectionAssert.AreEquivalent(new[] { "y" }, ((IEnumerable<string>)xSub.FirstLayerKeys).ToArray());

            Assert.IsFalse(trie.TryGetSubTrie("not_exist", out _));
        }

        [Test]
        public void Trie_Segment_RelativeWrite_ModifiesParent()
        {
            var trie = new Trie();
            trie.Add("root.branch.leaf1");

            // Locate the segment at root.branch.
            var seg = trie.GetSegment("root.branch");
            // Write a relative path under the segment without repeating the parent path.
            Assert.IsTrue(seg.Add("leaf2"));
            Assert.IsTrue(trie.Contains("root.branch.leaf2"));
        }

        [Test]
        public void Trie_Custom_Separator_Works()
        {
            var trie = new Trie('/');
            Assert.IsTrue(trie.Add("a/b/c"));
            Assert.IsTrue(trie.Contains("a/b/c"));
            Assert.IsTrue(trie.ContainsPartialKey("a/b"));
            Assert.IsTrue(trie.Remove("a/b/c"));
            Assert.IsFalse(trie.ContainsPartialKey("a/b"));
        }

        [Test]
        public void Trie_Clear_KeepStructure_Then_Shrink()
        {
            var trie = new Trie();
            trie.Add("a.b.c");
            trie.Add("a.b.d");

            // Clear the subtree while keeping the internal structure.
            Assert.IsTrue(trie.Clear("a.b", keepStructure: true));
            Assert.IsFalse(trie.ContainsPartialKey("a.b"));

            // Shrinking removes the empty branch from the visible trie.
            trie.Shrink();
            CollectionAssert.AreEquivalent(Array.Empty<string>(), ((IEnumerable<string>)trie.FirstLayerKeys).ToArray());
        }

        [Test]
        public void Trie_Clone_Is_DeepCopy_Expected()
        {
            var trie = new Trie();
            trie.Add("a.b.c");

            var clone = trie.Clone();
            Assert.IsTrue(clone.Contains("a.b.c"));

            // Mutating the clone should not affect the original trie.
            Assert.IsTrue(clone.Add("a.b.x"));
            Assert.IsFalse(trie.Contains("a.b.x"),
                "Deep clone expected: modifying clone should not affect original");

            // Mutating the original should not affect the clone either.
            Assert.IsTrue(trie.Add("root.node"));
            Assert.IsFalse(clone.Contains("root.node"));

            // If this fails, check whether Node.Clone copies child nodes correctly.
        }
    }

    public class TriesTests
    {
        [Test]
        public void Tries_Add_Get_TryGet_Indexer()
        {
            var tries = new Tries<int>();
            tries.Set("a.b", 10);
            tries.Set("a.c", 20);
            tries.Set("x.y", 30);

            Assert.IsTrue(tries.TryGetValue("a.b", out var v1) && v1 == 10);
            Assert.AreEqual(20, tries["a.c"]);
            Assert.AreEqual(30, tries.Get("x.y"));
            Assert.IsFalse(tries.TryGetValue("not.exist", out _));
        }

        [Test]
        public void Tries_Add_With_ListPrefix_And_Contains()
        {
            var tries = new Tries<string>();
            var path = new List<string> { "room", "kitchen", "sink" };
            tries.Set(path, "metal");
            Assert.IsTrue(tries.ContainsKey("room.kitchen.sink"));
            Assert.IsTrue(tries.TryGetValue(path, out var val) && val == "metal");
        }

        [Test]
        public void Tries_Keys_Values_ToDictionary()
        {
            var tries = new Tries<int>();
            tries.Set("a.b", 1);
            tries.Set("a.c", 2);
            tries.Set("x.y", 3);

            CollectionAssert.AreEquivalent(new[] { "a.b", "a.c", "x.y" }, ((IEnumerable<string>)tries.Keys).ToArray());
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, tries.Values.ToArray());

            var dict = tries.ToDictionary();
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(2, dict["a.c"]);
        }

        [Test]
        public void Tries_Remove_ByKey_And_ByKeyValue()
        {
            var tries = new Tries<int>();
            tries.Set("a.b", 1);
            tries.Set("a.c", 2);

            Assert.IsTrue(tries.Remove("a.b"));
            Assert.IsFalse(tries.ContainsKey("a.b"));

            // Remove by key/value pair.
            Assert.IsTrue(((IDictionary<string, int>)tries).Contains(new KeyValuePair<string, int>("a.c", 2)));
            Assert.IsTrue(((IDictionary<string, int>)tries).Remove(new KeyValuePair<string, int>("a.c", 2)),
                "Expected value-matched removal to succeed");

            Assert.IsFalse(tries.ContainsKey("a.c"));

            // If this fails, check Tries<T>.Node.Remove equality and condition logic.
        }

        [Test]
        public void Tries_Segment_RelativeWrite_And_TryGetSegment()
        {
            var tries = new Tries<int>();
            tries.Set("root.branch.leaf1", 5);

            var seg = tries.GetSegment("root.branch");
            seg.Set("leaf2", 9);
            Assert.IsTrue(tries.TryGetValue("root.branch.leaf2", out var v) && v == 9);

            Assert.IsTrue(tries.TryGetSegment("root", out var rootSeg));
            CollectionAssert.AreEquivalent(new[] { "branch" }, rootSeg.FirstLayerKeys.ToArray());
        }

        [Test]
        public void Tries_Clear_KeepStructure_Then_Shrink()
        {
            var tries = new Tries<int>();
            tries.Set("a.b.c", 1);
            tries.Set("a.b.d", 2);

            Assert.IsTrue(tries.Clear("a.b", keepStructure: true));
            Assert.IsFalse(tries.ContainsPartialKey("a.b"));

            tries.Shrink();
            CollectionAssert.AreEquivalent(Array.Empty<string>(), tries.FirstLayerKeys.ToArray());
        }

        [Test]
        public void Tries_Clone_Is_DeepCopy_Expected()
        {
            var tries = new Tries<int>();
            tries.Set("a.b", 1);

            var clone = tries.Clone();
            Assert.AreEqual(1, clone.Get("a.b"));

            clone.Set("a.c", 2);
            Assert.IsFalse(tries.ContainsKey("a.c"), "Deep clone expected: modifying clone should not affect original");

            tries.Set("x.y", 3);
            Assert.IsFalse(clone.ContainsKey("x.y"));

            // If this fails, check whether Tries<T>.Node.Clone copies child nodes correctly.
        }

        [Test]
        public void Tries_CustomSeparator_Works()
        {
            var tries = new Tries<string>('/');
            tries.Set("a/b/c", "ok");
            Assert.AreEqual("ok", tries.Get("a/b/c"));
            Assert.IsTrue(tries.ContainsPartialKey("a/b"));
            tries.Remove("a/b/c");
            Assert.IsFalse(tries.ContainsPartialKey("a/b"));
        }
    }

    public class TrieSegmentUtilityTests
    {
        [Test]
        public void TrieSegment_Split_Handles_Trailing_Separator_And_Empty()
        {
            var parts1 = TrieSegment.Split("a.b.c.", '.');
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, parts1);

            var parts2 = TrieSegment.Split("", '.');
            CollectionAssert.AreEqual(Array.Empty<string>(), parts2);
        }
    }

    public class TriesSegmentTests
    {
        private Tries<int> MakeTries()
        {
            var t = new Tries<int>();
            t.Set("a.b", 1);
            t.Set("a.c", 2);
            t.Set("x.y", 3);
            return t;
        }

        [Test]
        public void TriesSegment_BasicAddGetRemove()
        {
            var tries = MakeTries();
            var segA = tries.GetSegment("a");

            // relative get
            Assert.AreEqual(1, segA.Get("b"));
            Assert.AreEqual(2, segA["c"]);

            // relative set -> reflects in parent
            segA.Set("d", 4);
            Assert.AreEqual(4, tries.Get("a.d"));

            // relative remove -> reflects in parent
            Assert.IsTrue(segA.Remove("b"));
            Assert.IsFalse(tries.ContainsKey("a.b"));
        }

        [Test]
        public void TriesSegment_TryGetSegment_And_NestedWrites()
        {
            var tries = MakeTries();
            var rootSeg = new TriesSegment<int>(tries);

            // TryGetSegment from segment
            Assert.IsTrue(rootSeg.TryGetSegment("a", out var segA));
            Assert.IsTrue(segA.TryGetSegment("c", out var segC));

            // write on nested segment root (empty key means the node itself)
            segC.Set("", 99);
            Assert.AreEqual(99, tries.Get("a.c"));

            segA.Set(new List<string> { "e" }, value: 5); // relative path "a.e"
            Assert.AreEqual(5, tries.Get("a.e"));
        }

        [Test]
        public void TriesSegment_Keys_And_Values_Are_Relative_To_Segment()
        {
            var tries = MakeTries();
            var segA = tries.GetSegment("a");

            // "a" subtree has { b:1, c:2 }
            CollectionAssert.AreEquivalent(new[] { "b", "c" }, ((IEnumerable<string>)segA.Keys).ToArray());
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, segA.Values.ToArray());

            // dictionary-style enumeration (relative keys)
            var kvs = ((IEnumerable<KeyValuePair<string, int>>)segA).ToList();
            CollectionAssert.AreEquivalent(new[] { "b", "c" }, kvs.Select(kv => kv.Key).ToArray());
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, kvs.Select(kv => kv.Value).ToArray());
        }

        [Test]
        public void TriesSegment_Clear_KeepStructure_Then_Shrink_On_Parent()
        {
            var tries = MakeTries();
            var segA = tries.GetSegment("a");

            // clear the subtree but keep structure under "a"
            Assert.IsTrue(segA.Clear(keepStructure: true));
            Assert.AreEqual(0, segA.Count);

            // "a" still exists as an empty branch until parent shrinks
            tries.Shrink();
            CollectionAssert.AreEquivalent(new[] { "x" }, tries.FirstLayerKeys.ToArray());
        }

        [Test]
        public void TriesSegment_ToDictionary_Returns_Relative_Keys()
        {
            var tries = MakeTries();
            var segA = tries.GetSegment("a");
            var dict = segA.ToDictionary();

            // Keys are relative to the segment root: "b","c"
            CollectionAssert.AreEquivalent(new[] { "b", "c" }, dict.Keys.ToArray());
            Assert.AreEqual(1, dict["b"]);
            Assert.AreEqual(2, dict["c"]);
        }

        [Test]
        public void TriesSegment_Dictionary_Interfaces_Work()
        {
            var tries = MakeTries();
            var segA = tries.GetSegment("a");

            // IDictionary<string,T>.Contains / Remove with KeyValuePair
            Assert.IsTrue(segA.Contains(new KeyValuePair<string, int>("c", 2)));
            Assert.IsTrue(segA.Remove(new KeyValuePair<string, int>("c", 2)));
            Assert.IsFalse(tries.ContainsKey("a.c"));

            // IDictionary<string,T>.TryGetValue (relative)
            Assert.IsTrue(segA.TryGetValue("b", out var v) && v == 1);

            // Add via IDictionary<string,T>
            ((IDictionary<string, int>)segA).Add("k", 7);
            Assert.AreEqual(7, tries.Get("a.k"));
        }

        [Test]
        public void TriesSegment_KeyPointer_EmptyPath_Writes_On_Segment_Root()
        {
            var tries = MakeTries();                // a.b=1, a.c=2, x.y=3
            var segA = tries.GetSegment("a");

            // An empty path points to the segment root.
            segA.Set(new List<string>(), 42);
            Assert.AreEqual(42, tries.Get("a"));    // The value is written on the "a" node itself.
        }
    }
}
