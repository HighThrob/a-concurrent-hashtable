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
    public class WeakDictionaryStrongKeysTest
    {
        [TestMethod]
        public void WeakDictionaryStrongKeysInsert()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            Assert.AreEqual(value, dictionary[key], "Expected to retreive inserted item.");

            key = null;
            value = new object();

            dictionary.Insert(key, value);

            Assert.AreEqual(value, dictionary[null], "Expected to retreive item with null key.");

            key = new object();
            value = null;

            try
            {
                dictionary.Insert(key, value);
                Assert.Fail("Expected ArgumentNullException if value is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysGetOldest()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key = new object();
            var value = new object();

            var oldValue = dictionary.GetOldest(key, value);

            Assert.AreEqual(value, oldValue, "Unique insert, expected new value to be returned as 'old value'");

            value = new object();

            oldValue = dictionary.GetOldest(key, value);

            Assert.AreNotEqual(value, oldValue, "Duplicate insert, expected existing value, not new value, to be returned as 'old value'");

            key = new object();
            value = null;

            try
            {
                dictionary.GetOldest(key, value);
                Assert.Fail("Expected ArgumentNullException if value is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysRemove()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            dictionary.Remove(key);

            Assert.IsFalse(dictionary.TryGetValue(key, out value), "Expected not to find removed item.");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysTryGetValue()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            object retrievedValue;

            Assert.IsTrue(dictionary.TryGetValue(key, out retrievedValue), "Expected to find an inserted key");
            Assert.AreEqual(value, retrievedValue, "Expected found item to be equal to inserted item.");

            Assert.IsFalse(dictionary.TryGetValue(new object(), out retrievedValue), "Expected not to find a not inserted key");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysTryPopValue()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            object retrievedValue;

            Assert.IsFalse(dictionary.TryPopValue(new object(), out retrievedValue), "Expected not to find a not inserted key");
            Assert.IsTrue(dictionary.TryPopValue(key, out retrievedValue), "Expected to find an inserted key");
            Assert.AreEqual(value, retrievedValue, "Expected found item to be equal to inserted item.");
            Assert.IsFalse(dictionary.TryPopValue(key, out retrievedValue), "Expected not to find an a;ready poped key");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysIndexer()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key1 = new object();
            var value1 = new object();
            var key2 = new object();
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            Assert.AreEqual(value1, dictionary[key1], "Expected indexer with key to returns inserted item.");
            Assert.AreEqual(value2, dictionary[key2], "Expected indexer with key to returns inserted item.");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysCurrentValues()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key1 = new object();
            var value1 = new object();
            object key2 = null;
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            var currentValues = dictionary.GetCurrentValues();

            Assert.AreEqual(2, currentValues.Length, "Expected 2 items in the array.");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysCurrentKeys()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key1 = new object();
            var value1 = new object();
            object key2 = null;
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            var currentValues = dictionary.GetCurrentKeys();

            Assert.AreEqual(2, currentValues.Length, "Expected 2 items in the array.");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysClear()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            var key1 = new object();
            var value1 = new object();
            object key2 = null;
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            dictionary.Clear();

            var currentValues = dictionary.GetCurrentKeys();

            Assert.AreEqual(0, currentValues.Length, "Expected 0 items in the array after Clear.");
        }

        [TestMethod]
        public void WeakDictionaryStrongKeysGarbageCollection()
        {
            var dictionary = new WeakDictionaryStrongKeys<object, object>();

            for( int i = 0; i < 1000; ++i )
                dictionary[new Object()] = new object();

            GC.Collect();

            {
                int wait = 200;

                do
                { Thread.Sleep(100); }
                while (Interlocked.Decrement(ref wait) > 0 && dictionary.GetCurrentKeys().Length != 0);
            }

            var currentValues = dictionary.GetCurrentKeys();

            if( currentValues.Length != 0 )
                Assert.Inconclusive("Expected 0 items in the array after GC.");
        }
    }
}
