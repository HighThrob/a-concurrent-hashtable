using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConcurrentHashtable;
using SCG = System.Collections.Generic;
using System.Threading;

namespace ConcurrentHashtableUnitTest
{
    [TestClass]
    public class DictionaryTest
    {
        [TestMethod]
        public void DictionaryAdd()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            string result;

            Assert.IsTrue(dictionary.TryGetValue(10, out result), "Expected to find just inserted key.");
            Assert.AreEqual("ABC", result, "Expected found result to be equal to inserted item.");

            try
            {
                dictionary.Add(10, "ABC");
                Assert.Fail("Expected ArgumentException already present key.");
            }
            catch (ArgumentException)
            { }
        }

        [TestMethod]
        public void DictionaryContainsKey()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");

            Assert.IsTrue(dictionary.ContainsKey(10), "Expected to find just inserted key.");
            Assert.IsFalse(dictionary.ContainsKey(7), "Expected not to find a not inserted key.");
        }

        [TestMethod]
        public void DictionaryKeys()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            var keys = dictionary.Keys;

            Assert.AreEqual(2, keys.Count, "Expected two find nr of inserted unique keys in Keys collection.");
            Assert.IsTrue(keys.Contains(10), "Expected keys to contain inserted keys.");
            Assert.IsTrue(keys.Contains(2), "Expected keys to contain inserted keys.");
        }


        [TestMethod]
        public void DictionaryRemove()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            dictionary.Remove(10);
            dictionary.Remove(2);

            Assert.AreEqual(0, dictionary.Count, "Expected all items to be removed.");
        }

        [TestMethod]
        public void DictionaryTryGetValue()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");

            string result;

            Assert.IsTrue(dictionary.TryGetValue(10, out result), "Expected to find just inserted key.");
            Assert.AreEqual("ABC", result, "Expected found item to be equal to inserted item.");
            Assert.IsFalse(dictionary.TryGetValue(2, out result), "Expected not to find a not inserted key.");
        }

        [TestMethod]
        public void DictionaryValues()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            var values = dictionary.Values;

            Assert.AreEqual(2, values.Count, "Expected two find nr of inserted unique keys in Keys collection.");
            Assert.IsTrue(values.Contains("ABC"), "Expected keys to contain inserted keys.");
            Assert.IsTrue(values.Contains("DEF"), "Expected keys to contain inserted keys.");
        }

        [TestMethod]
        public void DictionaryIndexer()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary[10] = "QEW";
            dictionary[2] = "DEF";
            dictionary[10] = "ABC";

            Assert.AreEqual(2, dictionary.Count, "Expected two find nr of inserted unique keys in dictionary.");
            Assert.AreEqual("DEF", dictionary[2], "Expected indexer with key to returns inserted item.");
            Assert.AreEqual("ABC", dictionary[10], "Expected indexer with key to returns inserted item.");

            try
            {
                var dummy = dictionary[13];
                Assert.Fail("Expected KeyNotFoundException when accessing indexer with not inserted key.");
            }
            catch (KeyNotFoundException)
            { }
        }

        [TestMethod]
        public void DictionaryAdd2()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(new KeyValuePair<int,string>(10, "ABC"));
            string result;

            Assert.IsTrue(dictionary.TryGetValue(10, out result), "Expected to find just inserted key.");
            Assert.AreEqual("ABC", result, "Expected found result to be equal to inserted item.");

            try
            {
                dictionary.Add(new KeyValuePair<int, string>(10, "ABC"));
                Assert.Fail("Expected ArgumentException already present key.");
            }
            catch (ArgumentException)
            { }

        }

        [TestMethod]
        public void DictionaryClear()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count, "Expected dictionary to be empty after clear");
        }

        [TestMethod]
        public void DictionaryContains()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");

            Assert.IsTrue(dictionary.Contains( new KeyValuePair<int,string>( 10, "ABC") ), "Expected to find just inserted kvp.");
            Assert.IsFalse(dictionary.Contains(new KeyValuePair<int, string>(10, "DEF")), "Expected not to find not inserted kvp with inserted key (different item).");
            Assert.IsFalse(dictionary.Contains(new KeyValuePair<int, string>(2, "ABC")), "Expected not to find not inserted kvp with different key (same item).");
        }

        [TestMethod]
        public void DictionaryCopyTo()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            var array = new KeyValuePair<int,string>[4];

            dictionary.CopyTo(array, 1);
        }

        [TestMethod]
        public void DictionaryIsReadOnly()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();
            Assert.IsFalse(dictionary.IsReadOnly, "ConcurrentDictionary should be read-write.");
        }

        [TestMethod]
        public void DictionaryRemove2()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            dictionary.Remove(new KeyValuePair<int,string>(10,"ABC"));
            dictionary.Remove(new KeyValuePair<int, string>(2, "XYZ"));

            Assert.AreEqual(1, dictionary.Count, "Expected only exact matches to be removed.");
        }

        [TestMethod]
        public void DictionaryEnumerator()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            int c = 0;

            foreach (var kvp in dictionary)
            {
                c++;
            }

            Assert.AreEqual(2, c, "Expected 2 items in enumeration.");
        }

        [TestMethod]
        public void DictionaryEnumerator2()
        {
            var dictionary = new ConcurrentHashtable.ConcurrentDictionary<int, string>();

            dictionary.Add(10, "ABC");
            dictionary.Add(2, "DEF");

            int c = 0;

            foreach (var kvp in (System.Collections.IEnumerable)dictionary)
            {
                c++;
            }

            Assert.AreEqual(2, c, "Expected 2 items in enumeration.");
        }
    }
}
