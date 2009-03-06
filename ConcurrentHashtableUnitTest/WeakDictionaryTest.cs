/*  
 Copyright 2008 The 'A Concurrent Hashtable' development team  
 (http://www.codeplex.com/CH/People/ProjectPeople.aspx)

 This library is licensed under the GNU Library General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.codeplex.com/CH/license.
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SCG = System.Collections.Generic;
using System.Threading;
using System.IO;

namespace TvdP.Collections
{
    [TestClass]
    public class WeakDictionaryTest
    {
        [TestMethod]
        public void WeakDictionaryInsert()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            Assert.AreEqual(value, dictionary[key], "Expected to retreive inserted item.");

            key = new object();
            value = null;

            dictionary.Insert(key, value);

            Assert.AreEqual(null, dictionary[key], "Expected to retreive inserted null value.");


            key = null;
            value = new object();

            try
            {
                dictionary.Insert(key, value);
                Assert.Fail("Expected ArgumentNullException if key is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryGetOldest()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key = new object();
            var value = new object();

            var oldValue = dictionary.GetOldest(key, value);

            Assert.AreEqual(value, oldValue, "Unique insert, expected new value to be returned as 'old value'");

            value = new object();

            oldValue = dictionary.GetOldest(key, value);

            Assert.AreNotEqual(value, oldValue, "Duplicate insert, expected existing value, not new value, to be returned as 'old value'");

            key = null;
            value = new object();

            try
            {
                dictionary.GetOldest(key, value);
                Assert.Fail("Expected ArgumentNullException if key is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryRemove()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            dictionary.Remove(key);

            Assert.IsFalse(dictionary.TryGetValue(key, out value), "Expected not to find removed item.");

            key = null;

            try
            {
                dictionary.Remove(key);
                Assert.Fail("Expected ArgumentNullException if key is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryTryGetValue()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            object retrievedValue;

            Assert.IsTrue(dictionary.TryGetValue(key, out retrievedValue), "Expected to find an inserted key");
            Assert.AreEqual(value, retrievedValue, "Expected found item to be equal to inserted item.");

            Assert.IsFalse(dictionary.TryGetValue(new object(), out retrievedValue), "Expected not to find a not inserted key");

            try
            {
                dictionary.TryGetValue(null, out retrievedValue);
                Assert.Fail("Expected ArgumentNullException if key is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryTryPopValue()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key = new object();
            var value = new object();

            dictionary.Insert(key, value);

            object retrievedValue;

            Assert.IsFalse(dictionary.TryPopValue(new object(), out retrievedValue), "Expected not to find a not inserted key");
            Assert.IsTrue(dictionary.TryPopValue(key, out retrievedValue), "Expected to find an inserted key");
            Assert.AreEqual(value, retrievedValue, "Expected found item to be equal to inserted item.");
            Assert.IsFalse(dictionary.TryPopValue(key, out retrievedValue), "Expected not to find an a;ready poped key");

            try
            {
                dictionary.TryPopValue(null, out retrievedValue);
                Assert.Fail("Expected ArgumentNullException if key is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryIndexer()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key1 = new object();
            var value1 = new object();
            var key2 = new object();
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            Assert.AreEqual(value1, dictionary[key1], "Expected indexer with key to returns inserted item.");
            Assert.AreEqual(value2, dictionary[key2], "Expected indexer with key to returns inserted item.");

            try
            {
                var dummy = dictionary[null];
                Assert.Fail("Expected ArgumentNullException if key is null.");
            }
            catch (ArgumentNullException)
            { }
        }

        [TestMethod]
        public void WeakDictionaryCurrentValues()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key1 = new object();
            object value1 = null;
            var key2 = new object();
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            var currentValues = dictionary.GetCurrentValues();

            Assert.AreEqual(2, currentValues.Length, "Expected 2 items in the array.");
        }

        [TestMethod]
        public void WeakDictionaryCurrentKeys()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key1 = new object();
            object value1 = null;
            var key2 = new object();
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            var currentValues = dictionary.GetCurrentKeys();

            Assert.AreEqual(2, currentValues.Length, "Expected 2 items in the array.");
        }

        [TestMethod]
        public void WeakDictionaryClear()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var key1 = new object();
            object value1 = null;
            var key2 = new object();
            var value2 = new object();

            dictionary[key1] = new object();
            dictionary[key2] = value2;
            dictionary[key1] = value1;

            dictionary.Clear();

            var currentValues = dictionary.GetCurrentKeys();

            Assert.AreEqual(0, currentValues.Length, "Expected 0 items in the array after Clear.");
        }

        [TestMethod]
        public void WeakDictionaryGarbageCollection()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            for (int i = 0; i < 1000; ++i)
                dictionary[new Object()] = new object();

            GC.Collect();

            {
                int wait = 200;

                do
                { Thread.Sleep(100); }
                while (Interlocked.Decrement(ref wait) > 0 && dictionary.GetCurrentKeys().Length != 0);
            }

            var currentValues = dictionary.GetCurrentKeys();

            if (currentValues.Length != 0)
                Assert.Inconclusive("Expected 0 items in the array after GC.");
        }

        [TestMethod]
        public void WeakDictionarySerialization()
        {
            var dictionary = new ConcurrentWeakDictionary<object, object>();

            var items = new object[] {
                dictionary,
                10,2,
                "ABC",null 
            };

            dictionary.Insert(items[1], items[3]);
            dictionary.Insert(items[2], items[4]);

            var memStream = new MemoryStream();

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            formatter.Serialize(memStream, items);

            memStream.Seek(0, SeekOrigin.Begin);

            var itemsClone = (object[])formatter.Deserialize(memStream);

            var clone = (ConcurrentWeakDictionary<object, object>)itemsClone[0];

            Assert.AreEqual(itemsClone[3], clone[itemsClone[1]], "Item ABC not transfered");
            Assert.IsNull(clone[itemsClone[2]], "Item null not transfered");
        }
    }
}
